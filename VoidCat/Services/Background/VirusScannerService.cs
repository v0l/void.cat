using VoidCat.Services.Abstractions;
using VoidCat.Services.VirusScanner.Exceptions;

namespace VoidCat.Services.Background;

public class VirusScannerService : BackgroundService
{
    private readonly ILogger<VirusScannerService> _logger;
    private readonly IVirusScanner _scanner;
    private readonly IFileStore _fileStore;
    private readonly IVirusScanStore _scanStore;

    public VirusScannerService(ILogger<VirusScannerService> logger, IVirusScanner scanner, IVirusScanStore scanStore,
        IFileStore fileStore)
    {
        _scanner = scanner;
        _logger = logger;
        _scanStore = scanStore;
        _fileStore = fileStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Virus scanner background service starting..");

        while (!stoppingToken.IsCancellationRequested)
        {
            var page = 0;
            while (true)
            {
                var files = await _fileStore.ListFiles(new(page, 10));
                if (files.Pages < page) break;
                page++;

                await foreach (var file in files.Results.WithCancellation(stoppingToken))
                {
                    // check for scans
                    var scan = await _scanStore.Get(file.Id);
                    if (scan == default)
                    {
                        try
                        {
                            var result = await _scanner.ScanFile(file.Id, stoppingToken);
                            await _scanStore.Set(file.Id, result);
                        }
                        catch (RateLimitedException rx)
                        {
                            var sleep = rx.RetryAfter ?? DateTimeOffset.UtcNow.AddMinutes(10);
                            _logger.LogWarning("VirusScanner was rate limited, sleeping until {Time}", sleep);
                            await Task.Delay(sleep - DateTimeOffset.UtcNow, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to scan file {Id} error={Message}", file.Id, ex.Message);
                        }
                    }
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}