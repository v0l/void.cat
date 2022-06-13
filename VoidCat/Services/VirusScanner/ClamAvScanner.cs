using nClam;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.VirusScanner;

/// <summary>
/// ClamAV scanner
/// </summary>
public class ClamAvScanner : IVirusScanner
{
    private readonly ILogger<ClamAvScanner> _logger;
    private readonly IClamClient _clam;
    private readonly IFileStore _store;

    public ClamAvScanner(ILogger<ClamAvScanner> logger, IClamClient clam, IFileStore store)
    {
        _logger = logger;
        _clam = clam;
        _store = store;
    }

    /// <inheritdoc />
    public async ValueTask<VirusScanResult> ScanFile(Guid id, CancellationToken cts)
    {
        _logger.LogInformation("Starting scan of {Filename}", id);

        await using var fs = await _store.Open(new(id, Enumerable.Empty<RangeRequest>()), cts);
        var result = await _clam.SendAndScanFileAsync(fs, cts);

        if (result.Result == ClamScanResults.Error)
        {
            _logger.LogError("Failed to scan file {File}", id);
        }

        return new()
        {
            Id = Guid.NewGuid(),
            File = id,
            Score = result.Result == ClamScanResults.VirusDetected ? 1m : 0m,
            Names = string.Join(",", result.InfectedFiles?.Select(a => a.VirusName.Trim()) ?? Array.Empty<string>()),
            Scanner = "ClamAV"
        };
    }
}