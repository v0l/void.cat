using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Migrations;

/// <inheritdoc />
public class FixSize : IMigration
{
    private readonly ILogger<FixSize> _logger;
    private readonly IFileMetadataStore _fileMetadata;
    private readonly IFileStore _fileStore;

    public FixSize(ILogger<FixSize> logger, IFileMetadataStore fileMetadata, IFileStore fileStore)
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
            var fs = await _fileStore.Open(new(file.Id, Enumerable.Empty<RangeRequest>()), CancellationToken.None);
            if (file.Size != (ulong) fs.Length)
            {
                _logger.LogInformation("Updating file size {Id} to {Size}", file.Id, fs.Length);
                var newFile = file with
                {
                    Size = (ulong) fs.Length
                };
                await _fileMetadata.Set(newFile.Id, newFile);
            }
        }

        return IMigration.MigrationResult.Completed;
    }
}