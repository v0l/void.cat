using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services;

namespace VoidCat.Controllers
{
    [Route("stats")]
    public class StatsController : Controller
    {
        private readonly IStatsCollector _statsCollector;

        public StatsController(IStatsCollector statsCollector)
        {
            _statsCollector = statsCollector;
        }

        [HttpGet]
        public async Task<GlobalStats> GetGlobalStats()
        {
            var bw = await _statsCollector.GetBandwidth();
            return new(bw);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<FileStats> GetFileStats([FromRoute] string id)
        {
            var bw = await _statsCollector.GetBandwidth(id.FromBase58Guid());
            return new(bw);
        }
    }

    public sealed record GlobalStats(Bandwidth Bandwidth);
    public sealed record FileStats(Bandwidth Bandwidth);
}
