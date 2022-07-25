using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;

namespace VoidCat.Controllers
{
    [Route("upload")]
    public class UploadController : Controller
    {
        private readonly FileStoreFactory _storage;
        private readonly IFileMetadataStore _metadata;
        private readonly IPaywallStore _paywall;
        private readonly IPaywallFactory _paywallFactory;
        private readonly IFileInfoManager _fileInfo;
        private readonly IUserUploadsStore _userUploads;
        private readonly IUserStore _userStore;
        private readonly ITimeSeriesStatsReporter _timeSeriesStats;
        private readonly VoidSettings _settings;

        public UploadController(FileStoreFactory storage, IFileMetadataStore metadata, IPaywallStore paywall,
            IPaywallFactory paywallFactory, IFileInfoManager fileInfo, IUserUploadsStore userUploads,
            ITimeSeriesStatsReporter timeSeriesStats, IUserStore userStore, VoidSettings settings)
        {
            _storage = storage;
            _metadata = metadata;
            _paywall = paywall;
            _paywallFactory = paywallFactory;
            _fileInfo = fileInfo;
            _userUploads = userUploads;
            _timeSeriesStats = timeSeriesStats;
            _userStore = userStore;
            _settings = settings;
        }

        /// <summary>
        /// Primary upload endpoint
        /// </summary>
        /// <remarks>
        /// Additional optional headers can be included to provide details about the file being uploaded:
        ///
        /// `V-Content-Type` - Sets the `mimeType` of the file and is used on the preview page to display the file.
        /// `V-Filename` - Sets the filename of the file.
        /// `V-Description` - Sets the description of the file.
        /// `V-Full-Digest` - Include a SHA256 hash of the entire file for verification purposes.
        /// `V-Digest` - A SHA256 hash of the data you are sending in this request.
        /// </remarks>
        /// <param name="cli">True if you want to return only the url of the file in the response</param>
        /// <returns>Returns <see cref="UploadResult"/></returns>
        [HttpPost]
        [DisableRequestSizeLimit]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> UploadFile([FromQuery] bool cli = false)
        {
            try
            {
                var uid = HttpContext.GetUserId();
                var mime = Request.Headers.GetHeader("V-Content-Type");
                var filename = Request.Headers.GetHeader("V-Filename");
                if (string.IsNullOrEmpty(mime) && !string.IsNullOrEmpty(filename))
                {
                    if (new FileExtensionContentTypeProvider().TryGetContentType(filename, out var contentType))
                    {
                        mime = contentType;
                    }
                }

                // detect store for ingress
                var store = _settings.DefaultFileStore;
                if (uid.HasValue)
                {
                    var user = await _userStore.Get<InternalVoidUser>(uid.Value);
                    if (user?.Storage != default)
                    {
                        store = user.Storage!;
                    }
                }
                
                var meta = new SecretVoidFileMeta
                {
                    MimeType = mime,
                    Name = filename,
                    Description = Request.Headers.GetHeader("V-Description"),
                    Digest = Request.Headers.GetHeader("V-Full-Digest"),
                    Size = (ulong?)Request.ContentLength ?? 0UL,
                    Storage = store
                };

                var digest = Request.Headers.GetHeader("V-Digest");
                var vf = await _storage.Ingress(new(Request.Body, meta)
                {
                    Hash = digest
                }, HttpContext.RequestAborted);

                // save metadata
                await _metadata.Set(vf.Id, vf.Metadata!);

                // attach file upload to user
                if (uid.HasValue)
                {
                    await _userUploads.AddFile(uid!.Value, vf.Id);
                }

                if (cli)
                {
                    var urlBuilder = new UriBuilder(Request.IsHttps ? "https" : "http", Request.Host.Host,
                        Request.Host.Port ?? 80,
                        $"/d/{vf.Id.ToBase58()}");

                    return Content(urlBuilder.Uri.ToString(), "text/plain");
                }

                return Json(UploadResult.Success(vf));
            }
            catch (Exception ex)
            {
                return Json(UploadResult.Error(ex.Message));
            }
        }

        /// <summary>
        /// Append data onto a file
        /// </summary>
        /// <remarks>
        /// This endpoint is mainly used to bypass file upload limits enforced by CloudFlare.
        /// Clients should split their uploads into segments, upload the first segment to the regular
        /// upload endpoint, use the `editSecret` to append data to the file.
        ///
        /// Set the edit secret in the header `V-EditSecret` otherwise you will not be able to append data.
        /// </remarks>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [DisableRequestSizeLimit]
        [DisableFormValueModelBinding]
        [Route("{id}")]
        public async Task<UploadResult> UploadFileAppend([FromRoute] string id)
        {
            try
            {
                var gid = id.FromBase58Guid();
                var meta = await _metadata.Get<SecretVoidFileMeta>(gid);
                if (meta == default) return UploadResult.Error("File not found");

                var editSecret = Request.Headers.GetHeader("V-EditSecret");
                var digest = Request.Headers.GetHeader("V-Digest");
                var vf = await _storage.Ingress(new(Request.Body, meta)
                {
                    Hash = digest,
                    EditSecret = editSecret?.FromBase58Guid() ?? Guid.Empty,
                    Id = gid,
                    IsAppend = true
                }, HttpContext.RequestAborted);

                // update file size
                await _metadata.Set(vf.Id, vf.Metadata!);
                return UploadResult.Success(vf);
            }
            catch (Exception ex)
            {
                return UploadResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// Return information about a specific file
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetInfo([FromRoute] string id)
        {
            if (!id.TryFromBase58Guid(out var fid)) return StatusCode(404);

            var uid = HttpContext.GetUserId();
            var isOwner = uid.HasValue && await _userUploads.Uploader(fid) == uid;

            return isOwner ? Json(await _fileInfo.GetPrivate(fid)) : Json(await _fileInfo.Get(fid));
        }

        /// <summary>
        /// Return information about a specific file
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}/metrics")]
        public async Task<IActionResult> Metrics([FromRoute] string id)
        {
            if (!id.TryFromBase58Guid(out var fid)) return StatusCode(404);

            var stats = await _timeSeriesStats.GetBandwidth(fid, DateTime.UtcNow.Subtract(TimeSpan.FromDays(30)),
                DateTime.UtcNow);

            return Json(stats);
        }

        /// <summary>
        /// Create a paywall order to pay
        /// </summary>
        /// <param name="id">File id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}/paywall")]
        public async ValueTask<PaywallOrder?> CreateOrder([FromRoute] string id)
        {
            var gid = id.FromBase58Guid();
            var file = await _fileInfo.Get(gid);
            var config = await _paywall.Get(gid);

            var provider = await _paywallFactory.CreateProvider(config!.Service);
            return await provider.CreateOrder(file!.Paywall!);
        }

        /// <summary>
        /// Return the status of an order
        /// </summary>
        /// <param name="id">File id</param>
        /// <param name="order">Order id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}/paywall/{order:guid}")]
        public async ValueTask<PaywallOrder?> GetOrderStatus([FromRoute] string id, [FromRoute] Guid order)
        {
            var gid = id.FromBase58Guid();
            var config = await _paywall.Get(gid);

            var provider = await _paywallFactory.CreateProvider(config!.Service);
            return await provider.GetOrderStatus(order);
        }

        /// <summary>
        /// Update the paywall config
        /// </summary>
        /// <param name="id">File id</param>
        /// <param name="req">Requested config to set on the file</param>
        /// <returns></returns>
        [HttpPost]
        [Route("{id}/paywall")]
        public async Task<IActionResult> SetPaywallConfig([FromRoute] string id, [FromBody] SetPaywallConfigRequest req)
        {
            var gid = id.FromBase58Guid();
            var meta = await _metadata.Get<SecretVoidFileMeta>(gid);
            if (meta == default) return NotFound();
            if (!meta.CanEdit(req.EditSecret)) return Unauthorized();

            if (req.Strike != default)
            {
                await _paywall.Add(gid, new StrikePaywallConfig()
                {
                    Service = PaymentServices.Strike,
                    Handle = req.Strike.Handle,
                    Cost = req.Strike.Cost
                });

                return Ok();
            }

            // if none set, delete config
            await _paywall.Delete(gid);
            return Ok();
        }

        /// <summary>
        /// Update metadata about file
        /// </summary>
        /// <param name="id">Id of file to edit</param>
        /// <param name="fileMeta">New metadata to update</param>
        /// <returns></returns>
        /// <remarks>
        /// You can only change `Name`, `Description` and `MimeType`
        /// </remarks>
        [HttpPost]
        [Route("{id}/meta")]
        public async Task<IActionResult> UpdateFileMeta([FromRoute] string id, [FromBody] SecretVoidFileMeta fileMeta)
        {
            var gid = id.FromBase58Guid();
            var meta = await _metadata.Get<SecretVoidFileMeta>(gid);
            if (meta == default) return NotFound();
            if (!meta.CanEdit(fileMeta.EditSecret)) return Unauthorized();

            await _metadata.Update(gid, fileMeta);
            return Ok();
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var factories = context.ValueProviderFactories;
            factories.RemoveType<FormValueProviderFactory>();
            factories.RemoveType<FormFileValueProviderFactory>();
            factories.RemoveType<JQueryFormValueProviderFactory>();
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }

    public record UploadResult(bool Ok, PrivateVoidFile? File, string? ErrorMessage)
    {
        public static UploadResult Success(PrivateVoidFile vf)
            => new(true, vf, null);

        public static UploadResult Error(string message)
            => new(false, null, message);
    }

    public record SetPaywallConfigRequest
    {
        [JsonConverter(typeof(Base58GuidConverter))]
        public Guid EditSecret { get; init; }

        public StrikePaywallConfig? Strike { get; init; }
    }
}
