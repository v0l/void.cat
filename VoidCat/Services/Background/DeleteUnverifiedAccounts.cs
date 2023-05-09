using VoidCat.Database;
using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;

namespace VoidCat.Services.Background;

public class DeleteUnverifiedAccounts : BackgroundService
{
    private readonly ILogger<DeleteUnverifiedAccounts> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DeleteUnverifiedAccounts(ILogger<DeleteUnverifiedAccounts> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var userStore = scope.ServiceProvider.GetRequiredService<IUserStore>();
                var userUploads = scope.ServiceProvider.GetRequiredService<IUserUploadsStore>();
                var fileStore = scope.ServiceProvider.GetRequiredService<FileStoreFactory>();
                var fileInfoManager = scope.ServiceProvider.GetRequiredService<FileInfoManager>();

                var accounts = await userStore.ListUsers(new(0, Int32.MaxValue));

                await foreach (var account in accounts.Results.WithCancellation(stoppingToken))
                {
                    if (!account.Flags.HasFlag(UserFlags.EmailVerified) &&
                        account.Created.AddDays(7) < DateTimeOffset.UtcNow)
                    {
                        _logger.LogInformation("Deleting un-verified account: {Id}", account.Id.ToBase58());
                        await userStore.Delete(account.Id);

                        var files = await userUploads.ListFiles(account.Id, new(0, Int32.MinValue));
                        // ReSharper disable once UseCancellationTokenForIAsyncEnumerable
                        await foreach (var file in files.Results)
                        {
                            await fileStore.DeleteFile(file);
                            await fileInfoManager.Delete(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete unverified accounts");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}