using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services;

namespace VoidCat.Controllers
{
    [Route("upload")]
    public class UploadController : Controller
    {
        private readonly IFileIngressFactory _fileIngress;
        private readonly IStatsCollector _stats;

        public UploadController(IStatsCollector stats, IFileIngressFactory fileIngress)
        {
            _stats = stats;
            _fileIngress = fileIngress;
        }

        [HttpPost]
        public Task<VoidFile> UploadFile()
        {
            return Request.HasFormContentType ?
                saveFromForm() : _fileIngress.Ingress(Request.Body);
        }

        private Task<VoidFile> saveFromForm()
        {
            return Task.FromResult<VoidFile>(null);
        }
    }
}
