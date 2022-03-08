using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers
{
    [Route("upload")]
    public class UploadController : Controller
    {
        private readonly IFileStore _storage;
        private readonly IFileMetadataStore _metadata;
        private readonly IPaywallStore _paywall;
        private readonly IPaywallFactory _paywallFactory;
        private readonly IFileInfoManager _fileInfo;

        public UploadController(IFileStore storage, IFileMetadataStore metadata, IPaywallStore paywall,
            IPaywallFactory paywallFactory, IFileInfoManager fileInfo)
        {
            _storage = storage;
            _metadata = metadata;
            _paywall = paywall;
            _paywallFactory = paywallFactory;
            _fileInfo = fileInfo;
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
                var meta = new SecretVoidFileMeta()
                {
                    MimeType = Request.Headers.GetHeader("V-Content-Type"),
                    Name = Request.Headers.GetHeader("V-Filename"),
                    Description = Request.Headers.GetHeader("V-Description"),
                    Digest = Request.Headers.GetHeader("V-Full-Digest"),
                    Size = (ulong?)Request.ContentLength ?? 0UL,
                    Uploader = uid
                };

                var digest = Request.Headers.GetHeader("V-Digest");
                var vf = await _storage.Ingress(new(Request.Body, meta)
                {
                    Hash = digest
                }, HttpContext.RequestAborted);

                if (cli)
                {
                    var urlBuilder = new UriBuilder(Request.IsHttps ? "https" : "http", Request.Host.Host, Request.Host.Port ?? 80,
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
        public ValueTask<PublicVoidFile?> GetInfo([FromRoute] string id)
        {
            return _fileInfo.Get(id.FromBase58Guid());
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
            return await provider.CreateOrder(file!);
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

            if (req.EditSecret != meta.EditSecret) return Unauthorized();

            if (req.Strike != default)
            {
                await _paywall.Set(gid, req.Strike!);
                return Ok();
            }

            // if none set, set NoPaywallConfig
            await _paywall.Set(gid, new NoPaywallConfig());
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
