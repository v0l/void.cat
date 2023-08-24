using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;

namespace VoidCat.Services.Migrations;

public class CleanupLocalDiskStore : IMigration
{
    private readonly VoidSettings _settings;
    private readonly IFileMetadataStore _metadataStore;
    private readonly IFileStore _fileStore;
    private readonly ILogger<CleanupLocalDiskStore> _logger;

    public CleanupLocalDiskStore(VoidSettings settings, IFileMetadataStore store, ILogger<CleanupLocalDiskStore> logger,
        IFileStore fileStore)
    {
        _settings = settings;
        _metadataStore = store;
        _logger = logger;
        _fileStore = fileStore;
    }

    public int Order => 3;
    public async ValueTask<IMigration.MigrationResult> Migrate(string[] args)
    {
        if (_fileStore is not LocalDiskFileStore)
        {
            return IMigration.MigrationResult.Skipped;
        }

        await CleanupDisk();
        await CleanupMetadata();

        return IMigration.MigrationResult.Completed;
    }

    private async Task CleanupDisk()
    {
        var baseDir = Path.Join(_settings.DataDirectory, "files-v2");
        foreach (var path in Directory.EnumerateFiles(baseDir, "*.*", SearchOption.AllDirectories))
        {
            if (!Guid.TryParse(Path.GetFileNameWithoutExtension(path), out var id))
            {
                continue;
            }

            var meta = await _metadataStore.Get(id);
            if (meta == default)
            {
                _logger.LogInformation("Deleting unmapped file {Path}", path);
                File.Delete(path);
            }
        }
    }

    private async Task CleanupMetadata()
    {
        var page = 0;
        while (true)
        {
            var deleting = new List<Guid>();
            var fileList = await _metadataStore.ListFiles(new(page++, 1000));
            if (fileList.Results == 0) break;

            await foreach (var md in fileList.Data)
            {
                if (!await _fileStore.Exists(md.Id))
                {
                    deleting.Add(md.Id);
                }
            }

            foreach (var toDelete in deleting)
            {
                _logger.LogInformation("Deleting metadata with missing file {Id}", toDelete);
                await _metadataStore.Delete(toDelete);
            }
        }
    }
}
