using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

/// <inheritdoc />
public class LocalDiskFileMetadataStore : IFileMetadataStore
{
    private const string MetadataDir = "metadata-v3";
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

    /// <inheritdoc />
    public ValueTask<Database.File?> Get(Guid id)
    {
        return GetMeta<Database.File>(id);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<Database.File>> Get(Guid[] ids)
    {
        var ret = new List<Database.File>();
        foreach (var id in ids)
        {
            var r = await GetMeta<Database.File>(id);
            if (r != null)
            {
                ret.Add(r);
            }
        }

        return ret;
    }
    
    public ValueTask Add(Database.File f)
    {
        return Set(f.Id, f);
    }

    /// <inheritdoc />
    public async ValueTask Update(Guid id, Database.File meta)
    {
        var oldMeta = await Get(id);
        if (oldMeta == default) return;

        oldMeta.Patch(meta);
        await Set(id, oldMeta);
    }

    /// <inheritdoc />
    public ValueTask<PagedResult<Database.File>> ListFiles(PagedRequest request)
    {
        async IAsyncEnumerable<Database.File> EnumerateFiles()
        {
            foreach (var metaFile in
                     Directory.EnumerateFiles(Path.Join(_settings.DataDirectory, MetadataDir), "*.json"))
            {
                var json = await File.ReadAllTextAsync(metaFile);
                var meta = JsonConvert.DeserializeObject<Database.File>(json);
                if (meta != null)
                {
                    yield return meta;
                }
            }
        }

        var results = EnumerateFiles();
        results = (request.SortBy, request.SortOrder) switch
        {
            (PagedSortBy.Name, PageSortOrder.Asc) => results.OrderBy(a => a.Name),
            (PagedSortBy.Size, PageSortOrder.Asc) => results.OrderBy(a => a.Size),
            (PagedSortBy.Date, PageSortOrder.Asc) => results.OrderBy(a => a.Uploaded),
            (PagedSortBy.Name, PageSortOrder.Dsc) => results.OrderByDescending(a => a.Name),
            (PagedSortBy.Size, PageSortOrder.Dsc) => results.OrderByDescending(a => a.Size),
            (PagedSortBy.Date, PageSortOrder.Dsc) => results.OrderByDescending(a => a.Uploaded),
            _ => results
        };

        return ValueTask.FromResult(new PagedResult<Database.File>
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Data = results.Take(request.PageSize).Skip(request.Page * request.PageSize)
        });
    }

    /// <inheritdoc />
    public async ValueTask<IFileMetadataStore.StoreStats> Stats()
    {
        var files = await ListFiles(new(0, Int32.MaxValue));
        var count = await files.Data.CountAsync();
        var size = await files.Data.SumAsync(a => (long) a.Size);
        return new(count, (ulong) size);
    }

    /// <inheritdoc />
    public async ValueTask Set(Guid id, Database.File meta)
    {
        var path = MapMeta(id);
        var json = JsonConvert.SerializeObject(meta);
        await File.WriteAllTextAsync(path, json);
    }

    /// <inheritdoc />
    public ValueTask Delete(Guid id)
    {
        var path = MapMeta(id);
        if (File.Exists(path))
        {
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