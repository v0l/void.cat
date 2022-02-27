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

        [HttpPost]
        [DisableRequestSizeLimit]
        [DisableFormValueModelBinding]
        public async Task<UploadResult> UploadFile()
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
                    Uploader = uid
                };

                var digest = Request.Headers.GetHeader("V-Digest");
                var vf = await _storage.Ingress(new(Request.Body, meta, digest!), HttpContext.RequestAborted);

                return UploadResult.Success(vf);
            }
            catch (Exception ex)
            {
                return UploadResult.Error(ex.Message);
            }
        }

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
                var vf = await _storage.Ingress(new(Request.Body, meta, digest!)
                {
                    EditSecret = editSecret?.FromBase58Guid() ?? Guid.Empty,
                    Id = gid
                }, HttpContext.RequestAborted);

                return UploadResult.Success(vf);
            }
            catch (Exception ex)
            {
                return UploadResult.Error(ex.Message);
            }
        }

        [HttpGet]
        [Route("{id}")]
        public ValueTask<PublicVoidFile?> GetInfo([FromRoute] string id)
        {
            return _fileInfo.Get(id.FromBase58Guid());
        }

        [HttpGet]
        [Route("{id}/paywall")]
        public async ValueTask<PaywallOrder?> CreateOrder([FromRoute] string id)
        {
            var gid = id.FromBase58Guid();
            var file = await _fileInfo.Get(gid);
            var config = await _paywall.GetConfig(gid);

            var provider = await _paywallFactory.CreateProvider(config!.Service);
            return await provider.CreateOrder(file!);
        }

        [HttpGet]
        [Route("{id}/paywall/{order:guid}")]
        public async ValueTask<PaywallOrder?> GetOrderStatus([FromRoute] string id, [FromRoute] Guid order)
        {
            var gid = id.FromBase58Guid();
            var config = await _paywall.GetConfig(gid);

            var provider = await _paywallFactory.CreateProvider(config!.Service);
            return await provider.GetOrderStatus(order);
        }

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
                await _paywall.SetConfig(gid, req.Strike!);
                return Ok();
            }

            // if none set, set NoPaywallConfig
            await _paywall.SetConfig(gid, new NoPaywallConfig());
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
