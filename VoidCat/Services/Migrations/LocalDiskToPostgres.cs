using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;

namespace VoidCat.Services.Migrations;

public class LocalDiskToPostgres : IMigration
{
    private readonly ILogger<LocalDiskToPostgres> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly VoidSettings _settings;

    public LocalDiskToPostgres(VoidSettings settings, ILoggerFactory loggerFactory, IFileInfoManager fileInfoManager,
        IUserUploadsStore userUploadsStore, IAggregateStatsCollector statsCollector)
    {
        _logger = loggerFactory.CreateLogger<LocalDiskToPostgres>();
        _settings = settings;
        _loggerFactory = loggerFactory;
    }

    public async ValueTask<IMigration.MigrationResult> Migrate(string[] args)
    {
        if (!args.Contains("--migrate-local-to-postgres"))
        {
            return IMigration.MigrationResult.Skipped;
        }

        var metaStore =
            new LocalDiskFileMetadataStore(_settings, _loggerFactory.CreateLogger<LocalDiskFileMetadataStore>());

        var files = await metaStore.ListFiles<SecretVoidFileMeta>(new(0, Int32.MaxValue));
        await foreach (var file in files.Results)
        {
            _logger.LogInformation("Migrating file {File}", file.Id);
            try
            {
            }
            catch (Exception ex)
            {
            }
        }

        return IMigration.MigrationResult.ExitCompleted;
    }
}