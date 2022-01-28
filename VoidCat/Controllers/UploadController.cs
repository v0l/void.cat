using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Services;

namespace VoidCat.Controllers
{
    [Route("upload")]
    public class UploadController : Controller
    {
        private readonly IFileStorage _storage;

        public UploadController(IFileStorage storage)
        {
            _storage = storage;
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [DisableFormValueModelBinding]
        public Task<InternalVoidFile> UploadFile()
        {
            return Request.HasFormContentType ?
                saveFromForm() : _storage.Ingress(Request.Body, HttpContext.RequestAborted);
        }

        [HttpGet]
        [Route("{id}")]
        public Task<VoidFile?> GetInfo([FromRoute] string id)
        {
            return _storage.Get(id.FromBase58Guid());
        }
        
        [HttpPatch]
        [Route("{id}")]
        public Task UpdateFileInfo([FromRoute]string id, [FromBody]UpdateFileInfoRequest request)
        {
            return _storage.UpdateInfo(new VoidFile()
            {
                Id = id.FromBase58Guid(),
                Metadata = request.Metadata
            }, request.EditSecret);
        }

        private Task<InternalVoidFile> saveFromForm()
        {
            return Task.FromResult<InternalVoidFile>(null);
        }

        public record UpdateFileInfoRequest([JsonConverter(typeof(Base58GuidConverter))] Guid EditSecret, VoidFileMeta Metadata);
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
}
