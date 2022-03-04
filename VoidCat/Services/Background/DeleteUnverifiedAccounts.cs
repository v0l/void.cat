using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Background;

public class DeleteUnverifiedAccounts : BackgroundService
{
    private readonly ILogger<DeleteUnverifiedAccounts> _logger;
    private readonly IUserStore _userStore;
    private readonly IUserUploadsStore _userUploads;
    private readonly IFileInfoManager _fileInfo;
    private readonly IFileStore _fileStore;

    public DeleteUnverifiedAccounts(ILogger<DeleteUnverifiedAccounts> logger, IUserStore userStore,
        IUserUploadsStore uploadsStore, IFileInfoManager fileInfo, IFileStore fileStore)
    {
        _userStore = userStore;
        _logger = logger;
        _userUploads = uploadsStore;
        _fileInfo = fileInfo;
        _fileStore = fileStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var accounts = await _userStore.ListUsers(new(0, Int32.MaxValue));

        await foreach (var account in accounts.Results.WithCancellation(stoppingToken))
        {
            if (!account.Flags.HasFlag(VoidUserFlags.EmailVerified) &&
                account.Created.AddDays(7) < DateTimeOffset.UtcNow)
            {
                _logger.LogInformation("Deleting un-verified account: {Id}", account.Id.ToBase58());
                await _userStore.Delete(account);

                var files = await _userUploads.ListFiles(account.Id, new(0, Int32.MinValue));
                await foreach (var file in files.Results)
                {
                    await _fileStore.DeleteFile(file.Id);
                    await _fileInfo.Delete(file.Id);
                }
            }
        }

        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
    }
}