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
    private readonly IEnumerable<string> _oAuthProviders;

    public InfoController(IStatsReporter statsReporter, IFileMetadataStore fileMetadata, VoidSettings settings,
        ITimeSeriesStatsReporter stats, IEnumerable<IFileStore> fileStores, IEnumerable<IOAuthProvider> oAuthProviders)
    {
        _statsReporter = statsReporter;
        _fileMetadata = fileMetadata;
        _settings = settings;
        _timeSeriesStats = stats;
        _fileStores = fileStores.Select(a => a.Key);
        _oAuthProviders = oAuthProviders.Select(a => a.Id);
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

        return new()
        {
            Bandwidth = bw,
            TotalBytes = storeStats.Size,
            Count = storeStats.Files,
            BuildInfo = BuildInfo.GetBuildInfo(),
            CaptchaSiteKey = _settings.CaptchaSettings?.SiteKey,
            TimeSeriesMetrics = await _timeSeriesStats.GetBandwidth(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow),
            FileStores = _fileStores,
            UploadSegmentSize = _settings.UploadSegmentSize,
            OAuthProviders = _oAuthProviders
        };
    }

    public sealed class GlobalInfo
    {
        public Bandwidth Bandwidth { get; init; }
        public ulong TotalBytes { get; init; }
        public long Count { get; init; }
        public BuildInfo BuildInfo { get; init; }
        public string? CaptchaSiteKey { get; init; }
        public IEnumerable<BandwidthPoint> TimeSeriesMetrics { get; init; }
        public IEnumerable<string?> FileStores { get; init; }
        public ulong? UploadSegmentSize { get; init; }
        public IEnumerable<string> OAuthProviders { get; init; }
    }
}