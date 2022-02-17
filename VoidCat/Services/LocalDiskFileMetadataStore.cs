using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Model.Exceptions;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services;

public class LocalDiskFileMetadataStore : IFileMetadataStore
{
    private const string MetadataDir = "metadata-v2";
    private readonly VoidSettings _settings;
    
    public LocalDiskFileMetadataStore(VoidSettings settings)
    {
        _settings = settings;

        var metaPath = Path.Combine(_settings.DataDirectory, MetadataDir);
        if (!Directory.Exists(metaPath))
        {
            Directory.CreateDirectory(metaPath);
        }
    }
    
    public async ValueTask<SecretVoidFileMeta?> Get(Guid id)
    {
        var path = MapMeta(id);
        if (!File.Exists(path)) return default;

        var json = await File.ReadAllTextAsync(path);
        return JsonConvert.DeserializeObject<SecretVoidFileMeta>(json);
    }
    
    public async ValueTask Set(Guid id, SecretVoidFileMeta meta)
    {
        var path = MapMeta(id);
        var json = JsonConvert.SerializeObject(meta);
        await File.WriteAllTextAsync(path, json);
    }
    
    public async ValueTask Update(Guid id, SecretVoidFileMeta patch)
    {
        var oldMeta = await Get(id);
        if (oldMeta?.EditSecret != patch.EditSecret)
        {
            throw new VoidNotAllowedException("Edit secret incorrect");
        }

        await Set(id, patch);
    }

    private string MapMeta(Guid id) =>
        Path.ChangeExtension(Path.Join(_settings.DataDirectory, MetadataDir, id.ToString()), ".json");
}
