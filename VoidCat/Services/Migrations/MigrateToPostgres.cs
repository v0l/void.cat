using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;
using VoidCat.Services.Paywall;

namespace VoidCat.Services.Migrations;

public class MigrateToPostgres : IMigration
{
    private readonly ILogger<MigrateToPostgres> _logger;
    private readonly VoidSettings _settings;
    private readonly IFileMetadataStore _fileMetadata;
    private readonly ICache _cache;
    private readonly IPaywallStore _paywallStore;

    public MigrateToPostgres(VoidSettings settings, ILogger<MigrateToPostgres> logger, IFileMetadataStore fileMetadata,
        ICache cache, IPaywallStore paywallStore)
    {
        _logger = logger;
        _settings = settings;
        _fileMetadata = fileMetadata;
        _cache = cache;
        _paywallStore = paywallStore;
    }

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
}