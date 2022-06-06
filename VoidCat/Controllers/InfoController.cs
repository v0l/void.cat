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

    public InfoController(IStatsReporter statsReporter, IFileMetadataStore fileMetadata, VoidSettings settings)
    {
        _statsReporter = statsReporter;
        _fileMetadata = fileMetadata;
        _settings = settings;
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
        
        return new(bw, (ulong)storeStats.Size, storeStats.Files, BuildInfo.GetBuildInfo(), _settings.CaptchaSettings?.SiteKey);
    }

    public sealed record GlobalInfo(Bandwidth Bandwidth, ulong TotalBytes, long Count, BuildInfo BuildInfo,
        string? CaptchaSiteKey);
}