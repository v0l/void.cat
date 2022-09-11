using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;

namespace VoidCat.Services.Background;

/// <summary>
/// Delete expired files
/// </summary>
public sealed class DeleteExpiredFiles : BackgroundService
{
    private readonly ILogger<DeleteExpiredFiles> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DeleteExpiredFiles(ILogger<DeleteExpiredFiles> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var metadata = scope.ServiceProvider.GetRequiredService<IFileMetadataStore>();
            var fileInfoManager = scope.ServiceProvider.GetRequiredService<FileInfoManager>();
            var fileStoreFactory = scope.ServiceProvider.GetRequiredService<FileStoreFactory>();

            var files = await metadata.ListFiles<SecretFileMeta>(new(0, int.MaxValue));
            await foreach (var f in files.Results.WithCancellation(stoppingToken))
            {
                try
                {
                    if (f.Expires < DateTime.Now)
                    {
                        await fileStoreFactory.DeleteFile(f.Id);
                        await fileInfoManager.Delete(f.Id);

                        _logger.LogInformation("Deleted file: {Id}", f.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete file: {Id}", f.Id);
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}