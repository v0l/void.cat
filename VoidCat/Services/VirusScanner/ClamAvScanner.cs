using nClam;
using VoidCat.Database;
using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;

namespace VoidCat.Services.VirusScanner;

/// <summary>
/// ClamAV scanner
/// </summary>
public class ClamAvScanner : IVirusScanner
{
    private readonly ILogger<ClamAvScanner> _logger;
    private readonly IClamClient _clam;
    private readonly FileStoreFactory _fileSystemFactory;

    public ClamAvScanner(ILogger<ClamAvScanner> logger, IClamClient clam, FileStoreFactory fileSystemFactory)
    {
        _logger = logger;
        _clam = clam;
        _fileSystemFactory = fileSystemFactory;
    }

    /// <inheritdoc />
    public async ValueTask<VirusScanResult> ScanFile(Guid id, CancellationToken cts)
    {
        _logger.LogInformation("Starting scan of {Filename}", id);

        await using var fs = await _fileSystemFactory.Open(new(id, Enumerable.Empty<RangeRequest>()), cts);
        var result = await _clam.SendAndScanFileAsync(fs, cts);

        if (result.Result == ClamScanResults.Error)
        {
            _logger.LogError("Failed to scan file {File}", id);
        }

        return new()
        {
            Id = Guid.NewGuid(),
            FileId = id,
            Score = result.Result == ClamScanResults.VirusDetected ? 1m : 0m,
            Names = string.Join(",", result.InfectedFiles?.Select(a => a.VirusName.Trim()) ?? Array.Empty<string>()),
            Scanner = "ClamAV"
        };
    }
}