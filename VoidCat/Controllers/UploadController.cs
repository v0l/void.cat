using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Services;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers
{
    [Route("upload")]
    public class UploadController : Controller
    {
        private readonly IFileStore _storage;

        public UploadController(IFileStore storage)
        {
            _storage = storage;
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [DisableFormValueModelBinding]
        public async Task<UploadResult> UploadFile()
        {
            try
            {
                var meta = new VoidFileMeta()
                {
                    MimeType = Request.ContentType,
                    Name = Request.Headers.GetHeader("X-Filename")
                };

                var digest = Request.Headers.GetHeader("X-Digest");
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
                var fileInfo = await _storage.Get(gid);
                if (fileInfo == default) return UploadResult.Error("File not found");

                var editSecret = Request.Headers.GetHeader("X-EditSecret");
                var digest = Request.Headers.GetHeader("X-Digest");
                var vf = await _storage.Ingress(new(Request.Body, fileInfo.Metadata, digest!)
                {
                    EditSecret = editSecret?.FromBase58Guid(),
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
        public ValueTask<VoidFile?> GetInfo([FromRoute] string id)
        {
            return _storage.Get(id.FromBase58Guid());
        }

        [HttpPatch]
        [Route("{id}")]
        public ValueTask UpdateFileInfo([FromRoute] string id, [FromBody] UpdateFileInfoRequest request)
        {
            return _storage.UpdateInfo(new VoidFile()
            {
                Id = id.FromBase58Guid(),
                Metadata = request.Metadata
            }, request.EditSecret);
        }

        public record UpdateFileInfoRequest([JsonConverter(typeof(Base58GuidConverter))] Guid EditSecret,
            VoidFileMeta Metadata);
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

    public record UploadResult(bool Ok, InternalVoidFile? File, string? ErrorMessage)
    {
        public static UploadResult Success(InternalVoidFile vf)
            => new(true, vf, null);

        public static UploadResult Error(string message)
            => new(false, null, message);
    }
}