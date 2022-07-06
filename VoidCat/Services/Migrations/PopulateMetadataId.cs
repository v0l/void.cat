using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Migrations;

/// <inheritdoc />
public class PopulateMetadataId : IMigration
{
    private readonly IFileMetadataStore _metadataStore;

    public PopulateMetadataId(IFileMetadataStore metadataStore)
    {
        _metadataStore = metadataStore;
    }

    /// <inheritdoc />
    public int Order => 2;

    /// <inheritdoc />
    public async ValueTask<IMigration.MigrationResult> Migrate(string[] args)
    {
        if (!args.Contains("--add-metadata-id"))
        {
            return IMigration.MigrationResult.Skipped;
        }

        var files = await _metadataStore.ListFiles<SecretVoidFileMeta>(new(0, Int32.MaxValue));
        await foreach (var file in files.Results)
        {
            // read-write file metadata 
            await _metadataStore.Set(file.Id, file);
        }

        return IMigration.MigrationResult.ExitCompleted;
    }
}