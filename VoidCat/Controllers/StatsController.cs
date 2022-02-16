using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers
{
    [Route("stats")]
    public class StatsController : Controller
    {
        private readonly IStatsCollector _statsCollector;
        private readonly IFileStore _fileStore;

        public StatsController(IStatsCollector statsCollector, IFileStore fileStore)
        {
            _statsCollector = statsCollector;
            _fileStore = fileStore;
        }

        [HttpGet]
        public async Task<GlobalStats> GetGlobalStats()
        {
            var bw = await _statsCollector.GetBandwidth();
            var bytes = 0UL;
            await foreach (var vf in _fileStore.ListFiles())
            {
                bytes += vf.Size;
            }
            return new(bw, bytes);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<FileStats> GetFileStats([FromRoute] string id)
        {
            var bw = await _statsCollector.GetBandwidth(id.FromBase58Guid());
            return new(bw);
        }
    }

    public sealed record GlobalStats(Bandwidth Bandwidth, ulong TotalBytes);
    public sealed record FileStats(Bandwidth Bandwidth);
}
