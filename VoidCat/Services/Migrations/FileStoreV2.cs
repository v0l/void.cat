using VoidCat.Model;

namespace VoidCat.Services.Migrations;

public class FileStoreV2 : IMigration
{
    private readonly VoidSettings _settings;

    public FileStoreV2(VoidSettings settings)
    {
        _settings = settings;
    }

    public int Order => 2;
    public ValueTask<IMigration.MigrationResult> Migrate(string[] args)
    {
        var baseDir = Path.Join(_settings.DataDirectory, "files-v1");
        if (!Directory.Exists(baseDir))
        {
            return ValueTask.FromResult(IMigration.MigrationResult.Skipped);
        }

        foreach (var path in Directory.EnumerateFiles(baseDir))
        {
            if (!Guid.TryParse(Path.GetFileNameWithoutExtension(path), out var id))
            {
                continue;
            }

            var dest = MapPathV2(id);
            var destDir = Path.GetDirectoryName(dest)!;
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Move(MapPathV1(id), dest);
        }

        return ValueTask.FromResult(IMigration.MigrationResult.Completed);
    }

    private string MapPathV1(Guid id) =>
        Path.Join(_settings.DataDirectory, "files-v1", id.ToString());

    private string MapPathV2(Guid id) =>
        Path.Join(_settings.DataDirectory, "files-v2", id.ToString()[..2], id.ToString()[2..4], id.ToString());
}
