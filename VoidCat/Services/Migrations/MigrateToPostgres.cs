using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;
using VoidCat.Services.Paywall;
using VoidCat.Services.Users;

namespace VoidCat.Services.Migrations;

/// <inheritdoc />
public class MigrateToPostgres : IMigration
{
    private readonly ILogger<MigrateToPostgres> _logger;
    private readonly VoidSettings _settings;
    private readonly IFileMetadataStore _fileMetadata;
    private readonly ICache _cache;
    private readonly IPaywallStore _paywallStore;
    private readonly IUserStore _userStore;

    public MigrateToPostgres(VoidSettings settings, ILogger<MigrateToPostgres> logger, IFileMetadataStore fileMetadata,
        ICache cache, IPaywallStore paywallStore, IUserStore userStore)
    {
        _logger = logger;
        _settings = settings;
        _fileMetadata = fileMetadata;
        _cache = cache;
        _paywallStore = paywallStore;
        _userStore = userStore;
    }

    /// <inheritdoc />
    public async ValueTask<IMigration.MigrationResult> Migrate(string[] args)
    {
        if (args.Contains("--migrate-local-metadata-to-postgres"))
        {
            await MigrateFiles();
            return IMigration.MigrationResult.ExitCompleted;
        }

        if (args.Contains("--migrate-cache-paywall-to-postgres"))
        {
            await MigratePaywall();
            return IMigration.MigrationResult.ExitCompleted;
        }

        if (args.Contains("--migrate-cache-users-to-postgres"))
        {
            await MigrateUsers();
            return IMigration.MigrationResult.ExitCompleted;
        }

        return IMigration.MigrationResult.Skipped;
    }

    private async Task MigrateFiles()
    {
        var localDiskMetaStore =
            new LocalDiskFileMetadataStore(_settings);

        var files = await localDiskMetaStore.ListFiles<SecretVoidFileMeta>(new(0, int.MaxValue));
        await foreach (var file in files.Results)
        {
            try
            {
                file.MimeType ??= "application/octet-stream";
                await _fileMetadata.Set(file.Id, file);
                await localDiskMetaStore.Delete(file.Id);
                _logger.LogInformation("Migrated file metadata for {File}", file.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate file metadata for {File}", file.Id);
            }
        }
    }

    private async Task MigratePaywall()
    {
        var cachePaywallStore = new CachePaywallStore(_cache);

        var files = await _fileMetadata.ListFiles<VoidFileMeta>(new(0, int.MaxValue));
        await foreach (var file in files.Results)
        {
            try
            {
                var old = await cachePaywallStore.Get(file.Id);
                if (old != default)
                {
                    await _paywallStore.Add(file.Id, old);
                    _logger.LogInformation("Migrated paywall config for {File}", file.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate paywall config for {File}", file.Id);
            }
        }
    }

    private async Task MigrateUsers()
    {
        var cacheUsers = new CacheUserStore(_cache);

        var users = await cacheUsers.ListUsers(new(0, int.MaxValue));
        await foreach (var user in users.Results)
        {
            try
            {
                var privateUser = await cacheUsers.Get<PrivateUser>(user.Id);
                privateUser!.Password ??= privateUser.PasswordHash;
                
                await _userStore.Set(privateUser!.Id, new InternalVoidUser()
                {
                    Id = privateUser.Id,
                    Avatar = privateUser.Avatar,
                    Created = privateUser.Created,
                    DisplayName = privateUser.DisplayName,
                    Email = privateUser.Email,
                    Flags = privateUser.Flags,
                    LastLogin = privateUser.LastLogin,
                    Password = privateUser.Password!,
                    Roles = privateUser.Roles
                });
                _logger.LogInformation("Migrated user {USer}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate user {User}", user.Id);
            }
        }
    }

    private class PrivateUser : PrivateVoidUser
    {
        public string? PasswordHash { get; set; }
        public string? Password { get; set; }
    }
}