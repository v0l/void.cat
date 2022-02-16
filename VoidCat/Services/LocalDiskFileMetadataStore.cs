using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Model.Exceptions;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services;

public class LocalDiskFileMetadataStore : IFileMetadataStore
{
    private const string MetadataDir = "metadata";
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
    
    public async ValueTask<InternalVoidFile?> Get(Guid id)
    {
        var path = MapMeta(id);
        if (!File.Exists(path)) return default;

        var json = await File.ReadAllTextAsync(path);
        return JsonConvert.DeserializeObject<InternalVoidFile>(json);
    }
    
    public async ValueTask Set(InternalVoidFile meta)
    {
        var path = MapMeta(meta.Id);
        var json = JsonConvert.SerializeObject(meta);
        await File.WriteAllTextAsync(path, json);
    }
    
    public async ValueTask Update(VoidFile patch, Guid editSecret)
    {
        var oldMeta = await Get(patch.Id);
        if (oldMeta?.EditSecret != editSecret)
        {
            throw new VoidNotAllowedException("Edit secret incorrect");
        }

        // only patch metadata
        oldMeta.Metadata = patch.Metadata;

        await Set(oldMeta);
    }

    private string MapMeta(Guid id) =>
        Path.ChangeExtension(Path.Join(_settings.DataDirectory, MetadataDir, id.ToString()), ".json");
}
