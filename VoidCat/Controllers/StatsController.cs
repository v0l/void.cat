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

    public sealed record FileStats(Bandwidth Bandwidth);
}