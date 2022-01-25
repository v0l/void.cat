using Microsoft.AspNetCore.Mvc;
using System.Buffers;
using VoidCat.Model;
using VoidCat.Services;

namespace VoidCat.Controllers
{
    [Route("upload")]
    public class UploadController : Controller
    {
        private readonly IStatsCollector _stats;

        public UploadController(IStatsCollector stats)
        {
            _stats = stats;
        }

        [HttpPost]
        public Task<VoidFile> UploadFile()
        {
            return Request.HasFormContentType ?
                saveFromForm() : saveFromBody();
        }

        private async Task<VoidFile> saveFromBody()
        {
            var temp = Path.GetTempFileName();
            using var fsTemp = new FileStream(temp, FileMode.Create, FileAccess.ReadWrite);

            var buffer = MemoryPool<byte>.Shared.Rent();
            var rlen = 0;
            while ((rlen = await Request.Body.ReadAsync(buffer.Memory)) > 0)
            {
               
            }
            return default;
        }

        private Task<VoidFile> saveFromForm()
        {
            return Task.FromResult<VoidFile>(null);
        }
    }
}
