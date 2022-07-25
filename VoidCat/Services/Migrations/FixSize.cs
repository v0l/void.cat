using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;

namespace VoidCat.Services.Migrations;

/// <inheritdoc />
public class FixSize : IMigration
{
    private readonly ILogger<FixSize> _logger;
    private readonly IFileMetadataStore _fileMetadata;
    private readonly FileStoreFactory _fileStore;

    public FixSize(ILogger<FixSize> logger, IFileMetadataStore fileMetadata, FileStoreFactory fileStore)
    {
        _logger = logger;
        _fileMetadata = fileMetadata;
        _fileStore = fileStore;
    }

    /// <inheritdoc />
    public int Order => 2;

    /// <inheritdoc />
    public async ValueTask<IMigration.MigrationResult> Migrate(string[] args)
    {
        var files = await _fileMetadata.ListFiles<SecretVoidFileMeta>(new(0, int.MaxValue));
        await foreach (var file in files.Results)
        {
            try
            {
                var fs = await _fileStore.Open(new(file.Id, Enumerable.Empty<RangeRequest>()), CancellationToken.None);
                if (file.Size != (ulong)fs.Length)
                {
                    _logger.LogInformation("Updating file size {Id} to {Size}", file.Id, fs.Length);
                    var newFile = file with
                    {
                        Size = (ulong)fs.Length
                    };

                    await _fileMetadata.Set(newFile.Id, newFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fix file {id}", file.Id);
            }
        }

        return IMigration.MigrationResult.Completed;
    }
}