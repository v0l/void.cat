using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers
{
    [Route("stats")]
    public class StatsController : Controller
    {
        private readonly IStatsReporter _statsReporter;
        private readonly IFileStore _fileStore;

        public StatsController(IStatsReporter statsReporter, IFileStore fileStore)
        {
            _statsReporter = statsReporter;
            _fileStore = fileStore;
        }

        
        /// <summary>
        /// Return system info
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
        public async Task<GlobalStats> GetGlobalStats()
        {
            var bw = await _statsReporter.GetBandwidth();
            var bytes = 0UL;
            var count = 0;
            var files = await _fileStore.ListFiles(new(0, Int32.MaxValue));
            await foreach (var vf in files.Results)
            {
                bytes += vf.Metadata?.Size ?? 0;
                count++;
            }

            return new(bw, bytes, count, BuildInfo.GetBuildInfo());
        }

        /// <summary>
        /// Get stats for a specific file
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        public async Task<FileStats> GetFileStats([FromRoute] string id)
        {
            var bw = await _statsReporter.GetBandwidth(id.FromBase58Guid());
            return new(bw);
        }
    }

    public sealed record GlobalStats(Bandwidth Bandwidth, ulong TotalBytes, int Count, BuildInfo BuildInfo);

    public sealed record FileStats(Bandwidth Bandwidth);
}