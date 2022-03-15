﻿using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers;

[Route("info")]
public class InfoController : Controller
{
    private readonly IStatsReporter _statsReporter;
    private readonly IFileStore _fileStore;
    private readonly VoidSettings _settings;

    public InfoController(IStatsReporter statsReporter, IFileStore fileStore, VoidSettings settings)
    {
        _statsReporter = statsReporter;
        _fileStore = fileStore;
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
        var bytes = 0UL;
        var count = 0;
        var files = await _fileStore.ListFiles(new(0, Int32.MaxValue));
        await foreach (var vf in files.Results)
        {
            bytes += vf.Metadata?.Size ?? 0;
            count++;
        }

        return new(bw, bytes, count, BuildInfo.GetBuildInfo(), _settings.CaptchaSettings?.SiteKey);
    }

    public sealed record GlobalInfo(Bandwidth Bandwidth, ulong TotalBytes, int Count, BuildInfo BuildInfo,
        string? CaptchaSiteKey);
}