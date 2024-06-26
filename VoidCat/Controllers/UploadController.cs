﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using VoidCat.Database;
using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;
using VoidCat.Services.Users;
using File = VoidCat.Database.File;

namespace VoidCat.Controllers
{
    [Route("upload")]
    public class UploadController : Controller
    {
        private readonly FileStoreFactory _storage;
        private readonly IFileMetadataStore _metadata;
        private readonly IPaymentStore _paymentStore;
        private readonly IPaymentFactory _paymentFactory;
        private readonly FileInfoManager _fileInfo;
        private readonly IUserUploadsStore _userUploads;
        private readonly IUserStore _userStore;
        private readonly UserManager _userManager;
        private readonly ITimeSeriesStatsReporter _timeSeriesStats;
        private readonly VoidSettings _settings;

        public UploadController(FileStoreFactory storage, IFileMetadataStore metadata, IPaymentStore payment,
            IPaymentFactory paymentFactory, FileInfoManager fileInfo, IUserUploadsStore userUploads,
            ITimeSeriesStatsReporter timeSeriesStats, IUserStore userStore, VoidSettings settings, UserManager userManager)
        {
            _storage = storage;
            _metadata = metadata;
            _paymentStore = payment;
            _paymentFactory = paymentFactory;
            _fileInfo = fileInfo;
            _userUploads = userUploads;
            _timeSeriesStats = timeSeriesStats;
            _userStore = userStore;
            _settings = settings;
            _userManager = userManager;
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
        /// </remarks>
        /// <param name="cli">True if you want to return only the url of the file in the response</param>
        /// <returns>Returns <see cref="UploadResult"/></returns>
        [HttpPost]
        [HttpOptions]
        [HttpHead]
        [DisableRequestSizeLimit]
        [DisableFormValueModelBinding]
        [Authorize(AuthenticationSchemes = "Bearer,Nostr", Policy = Policies.RequireNostr)]
        [AllowAnonymous]
        public async Task<IActionResult> UploadFile([FromQuery] bool cli = false)
        {
            try
            {
                var mime = Request.Headers.GetHeader("V-Content-Type");
                var stripMetadata = (mime?.StartsWith("image/") ?? false) || (Request.Headers.GetHeader("V-Strip-Metadata")
                    ?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false);

                if (_settings.MaintenanceMode)
                {
                    throw new InvalidOperationException("Site is in maintenance mode");
                }

                var bodyLength = Request.ContentLength ?? 0;
                if ((ulong)bodyLength >= (_settings.MaxFileSize ?? ulong.MaxValue))
                {
                    throw new InvalidOperationException("File size too large");
                }

                var uid = HttpContext.GetUserId();
                var pubkey = HttpContext.GetPubKey();
                if (uid == default && !string.IsNullOrEmpty(pubkey))
                {
                    var nostrUser = await _userManager.LoginOrRegister(pubkey);
                    uid = nostrUser.Id;
                }

                var filename = Request.Headers.GetHeader("V-Filename");

                if (string.IsNullOrEmpty(mime) && !string.IsNullOrEmpty(filename))
                {
                    if (new FileExtensionContentTypeProvider().TryGetContentType(filename, out var contentType))
                    {
                        mime = contentType;
                    }
                }

                if (string.IsNullOrEmpty(mime))
                {
                    mime = "application/octet-stream";
                }

                // detect store for ingress
                var store = _settings.DefaultFileStore;
                if (uid.HasValue)
                {
                    var user = await _userStore.Get(uid.Value);
                    if (user?.Storage != default)
                    {
                        store = user.Storage!;
                    }
                }

                var meta = new File
                {
                    MimeType = mime,
                    Name = filename,
                    Description = Request.Headers.GetHeader("V-Description"),
                    Digest = Request.Headers.GetHeader("V-Full-Digest"),
                    Size = (ulong?)Request.ContentLength ?? 0UL,
                    Storage = store,
                    EncryptionParams = Request.Headers.GetHeader("V-EncryptionParams")
                };

                var (segment, totalSegments) = ParseSegmentsHeader();
                var vf = await _storage.Ingress(new(Request.Body, meta, segment, totalSegments, stripMetadata),
                    HttpContext.RequestAborted);

                // save metadata
                await _metadata.Add(vf);

                // attach file upload to user
                if (uid.HasValue)
                {
                    await _userUploads.AddFile(uid.Value, vf.Id);
                }

                if (cli)
                {
                    var urlBuilder = new UriBuilder(_settings.SiteUrl)
                    {
                        Path = $"/d/{vf.Id.ToBase58()}"
                    };

                    return Content(urlBuilder.Uri.ToString(), "text/plain");
                }

                return Json(UploadResult.Success(vf.ToResponse(true)));
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
                var stripMetadata = Request.Headers.GetHeader("V-Strip-Metadata")
                    ?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false;

                if (_settings.MaintenanceMode && !stripMetadata)
                {
                    throw new InvalidOperationException("Site is in maintenance mode");
                }

                var gid = id.FromBase58Guid();
                var meta = await _metadata.Get(gid);
                if (meta == default) return UploadResult.Error("File not found");

                // Parse V-Segment header
                var (segment, totalSegments) = ParseSegmentsHeader();

                // sanity check for append operations
                if (segment <= 1 || totalSegments <= 1)
                {
                    return UploadResult.Error("Malformed request, segment must be > 1 for append");
                }

                var editSecret = Request.Headers.GetHeader("V-EditSecret");
                var vf = await _storage.Ingress(new(Request.Body, meta, segment, totalSegments, stripMetadata)
                {
                    EditSecret = editSecret?.FromBase58Guid() ?? Guid.Empty,
                    Id = gid
                }, HttpContext.RequestAborted);

                // update file size
                await _metadata.Update(vf.Id, vf);
                return UploadResult.Success(vf.ToResponse(true));
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

            var info = await _fileInfo.Get(fid, isOwner || HttpContext.IsRole(Roles.Admin));
            if (info == default) return StatusCode(404);

            return Json(info);
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
        /// Create a payment order to pay
        /// </summary>
        /// <param name="id">File id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}/payment")]
        public async ValueTask<PaywallOrder?> CreateOrder([FromRoute] string id)
        {
            var gid = id.FromBase58Guid();
            var file = await _fileInfo.Get(gid, false);
            var config = await _paymentStore.Get(gid);

            var provider = await _paymentFactory.CreateProvider(config!.Service);
            return await provider.CreateOrder(file!.Payment!);
        }

        /// <summary>
        /// Return the status of an order
        /// </summary>
        /// <param name="id">File id</param>
        /// <param name="order">Order id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}/payment/{order:guid}")]
        public async ValueTask<PaywallOrder?> GetOrderStatus([FromRoute] string id, [FromRoute] Guid order)
        {
            var gid = id.FromBase58Guid();
            var config = await _paymentStore.Get(gid);

            var provider = await _paymentFactory.CreateProvider(config!.Service);
            return await provider.GetOrderStatus(order);
        }

        /// <summary>
        /// Update the payment config
        /// </summary>
        /// <param name="id">File id</param>
        /// <param name="req">Requested config to set on the file</param>
        /// <returns></returns>
        [HttpPost]
        [Route("{id}/payment")]
        public async Task<IActionResult> SetPaymentConfig([FromRoute] string id, [FromBody] SetPaymentConfigRequest req)
        {
            var gid = id.FromBase58Guid();
            var meta = await _metadata.Get(gid);
            if (meta == default) return NotFound();
            if (!meta.CanEdit(req.EditSecret)) return Unauthorized();

            if (req.StrikeHandle != default)
            {
                if (meta.Paywall?.Id != default)
                {
                    await _paymentStore.Delete(meta.Paywall.Id);
                }

                await _paymentStore.Add(gid, new Paywall
                {
                    FileId = meta.Id,
                    Service = PaywallService.Strike,
                    Upstream = req.StrikeHandle,
                    Amount = req.Amount,
                    Currency = Enum.Parse<PaywallCurrency>(req.Currency),
                    Required = req.Required
                });

                return Ok();
            }

            // if none set, delete existing config
            if (meta.Paywall?.Id != default)
            {
                await _paymentStore.Delete(meta.Paywall.Id);
            }

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
        public async Task<IActionResult> UpdateFileMeta([FromRoute] string id, [FromBody] VoidFileMeta fileMeta)
        {
            var gid = id.FromBase58Guid();
            var meta = await _metadata.Get(gid);
            if (meta == default) return NotFound();
            if (!(meta.CanEdit(fileMeta.EditSecret) || HttpContext.IsRole(Roles.Admin))) return Unauthorized();

            await _metadata.Update(gid, new()
            {
                Name = fileMeta.Name,
                Description = fileMeta.Description,
                Expires = fileMeta.Expires,
                MimeType = fileMeta.MimeType
            });

            return Ok();
        }

        private (int Segment, int TotalSegments) ParseSegmentsHeader()
        {
            // Parse V-Segment header
            int segment = 1, totalSegments = 1;
            var segmentHeader = Request.Headers.GetHeader("V-Segment");
            if (!string.IsNullOrEmpty(segmentHeader))
            {
                var split = segmentHeader.Split("/");
                if (split.Length == 2 && int.TryParse(split[0], out var a) && int.TryParse(split[1], out var b))
                {
                    segment = a;
                    totalSegments = b;
                }
            }

            return (segment, totalSegments);
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

    public record UploadResult(bool Ok, VoidFileResponse? File, string? ErrorMessage)
    {
        public static UploadResult Success(VoidFileResponse vf)
            => new(true, vf, null);

        public static UploadResult Error(string message)
            => new(false, null, message);
    }

    public record SetPaymentConfigRequest
    {
        [JsonConverter(typeof(Base58GuidConverter))]
        public Guid EditSecret { get; init; }

        public decimal Amount { get; init; }

        public string Currency { get; init; } = null!;

        public string? StrikeHandle { get; init; }

        public bool Required { get; init; }
    }
}
