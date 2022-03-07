using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Model.Exceptions;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

public class LocalDiskFileMetadataStore : IFileMetadataStore
{
    private const string MetadataDir = "metadata-v3";
    private readonly ILogger<LocalDiskFileMetadataStore> _logger;
    private readonly VoidSettings _settings;

    public LocalDiskFileMetadataStore(VoidSettings settings, ILogger<LocalDiskFileMetadataStore> logger)
    {
        _settings = settings;
        _logger = logger;

        var metaPath = Path.Combine(_settings.DataDirectory, MetadataDir);
        if (!Directory.Exists(metaPath))
        {
            Directory.CreateDirectory(metaPath);
        }
    }

    public ValueTask<TMeta?> Get<TMeta>(Guid id) where TMeta : VoidFileMeta
    {
        return GetMeta<TMeta>(id);
    }

    public ValueTask<VoidFileMeta?> Get(Guid id)
    {
        return GetMeta<VoidFileMeta>(id);
    }

    public async ValueTask Set(Guid id, SecretVoidFileMeta meta)
    {
        var path = MapMeta(id);
        var json = JsonConvert.SerializeObject(meta);
        await File.WriteAllTextAsync(path, json);
    }

    public ValueTask Delete(Guid id)
    {
        var path = MapMeta(id);
        if (File.Exists(path))
        {
            _logger.LogInformation("Deleting metadata file {Path}", path);
            File.Delete(path);
        }

        return ValueTask.CompletedTask;
    }

    private async ValueTask<TMeta?> GetMeta<TMeta>(Guid id)
    {
        var path = MapMeta(id);
        if (!File.Exists(path)) return default;

        var json = await File.ReadAllTextAsync(path);
        return JsonConvert.DeserializeObject<TMeta>(json);
    }

    private string MapMeta(Guid id) =>
        Path.ChangeExtension(Path.Join(_settings.DataDirectory, MetadataDir, id.ToString()), ".json");
}
