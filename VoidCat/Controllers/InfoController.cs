using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers;

[Route("info")]
public class InfoController : Controller
{
    private readonly IStatsReporter _statsReporter;
    private readonly IFileMetadataStore _fileMetadata;
    private readonly VoidSettings _settings;
    private readonly ITimeSeriesStatsReporter _timeSeriesStats;
    private readonly IEnumerable<string?> _fileStores;

    public InfoController(IStatsReporter statsReporter, IFileMetadataStore fileMetadata, VoidSettings settings,
        ITimeSeriesStatsReporter stats, IEnumerable<IFileStore> fileStores)
    {
        _statsReporter = statsReporter;
        _fileMetadata = fileMetadata;
        _settings = settings;
        _timeSeriesStats = stats;
        _fileStores = fileStores.Select(a => a.Key);
    }

    /// <summary>
    /// Return system info
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
    public async Task<GlobalInfo> GetGlobalStats()
    {
        var bw = await _statsReporter.GetBandwidth();
        var storeStats = await _fileMetadata.Stats();

        return new(bw, storeStats.Size, storeStats.Files, BuildInfo.GetBuildInfo(),
            _settings.CaptchaSettings?.SiteKey,
            await _timeSeriesStats.GetBandwidth(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow),
            _fileStores);
    }

    public sealed record GlobalInfo(Bandwidth Bandwidth, ulong TotalBytes, long Count, BuildInfo BuildInfo,
        string? CaptchaSiteKey, IEnumerable<BandwidthPoint> TimeSeriesMetrics, IEnumerable<string?> FileStores);
}
