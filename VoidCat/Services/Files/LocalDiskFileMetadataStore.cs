using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

/// <inheritdoc />
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

    /// <inheritdoc />
    public ValueTask<TMeta?> Get<TMeta>(Guid id) where TMeta : VoidFileMeta
    {
        return GetMeta<TMeta>(id);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<TMeta>> Get<TMeta>(Guid[] ids) where TMeta : VoidFileMeta
    {
        var ret = new List<TMeta>();
        foreach (var id in ids)
        {
            var r = await GetMeta<TMeta>(id);
            if (r != null)
            {
                ret.Add(r);
            }
        }

        return ret;
    }

    /// <inheritdoc />
    public async ValueTask Update<TMeta>(Guid id, TMeta meta) where TMeta : VoidFileMeta
    {
        var oldMeta = await Get<SecretVoidFileMeta>(id);
        if (oldMeta == default) return;

        oldMeta.Description = meta.Description ?? oldMeta.Description;
        oldMeta.Name = meta.Name ?? oldMeta.Name;
        oldMeta.MimeType = meta.MimeType ?? oldMeta.MimeType;

        await Set(id, oldMeta);
    }

    /// <inheritdoc />
    public ValueTask<PagedResult<TMeta>> ListFiles<TMeta>(PagedRequest request) where TMeta : VoidFileMeta
    {
        async IAsyncEnumerable<TMeta> EnumerateFiles()
        {
            foreach (var metaFile in
                     Directory.EnumerateFiles(Path.Join(_settings.DataDirectory, MetadataDir), "*.json"))
            {
                var json = await File.ReadAllTextAsync(metaFile);
                var meta = JsonConvert.DeserializeObject<TMeta>(json);
                if (meta != null)
                {
                    yield return meta with
                    {
                        // TODO: remove after migration decay
                        Id = Guid.Parse(Path.GetFileNameWithoutExtension(metaFile))
                    };
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

        return ValueTask.FromResult(new PagedResult<TMeta>
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Results = results.Take(request.PageSize).Skip(request.Page * request.PageSize)
        });
    }

    /// <inheritdoc />
    public async ValueTask<IFileMetadataStore.StoreStats> Stats()
    {
        var files = await ListFiles<VoidFileMeta>(new(0, Int32.MaxValue));
        var count = await files.Results.CountAsync();
        var size = await files.Results.SumAsync(a => (long) a.Size);
        return new(count, (ulong) size);
    }

    /// <inheritdoc />
    public ValueTask<VoidFileMeta?> Get(Guid id)
    {
        return GetMeta<VoidFileMeta>(id);
    }

    /// <inheritdoc />
    public ValueTask<SecretVoidFileMeta?> GetPrivate(Guid id)
    {
        return GetMeta<SecretVoidFileMeta>(id);
    }

    /// <inheritdoc />
    public async ValueTask Set(Guid id, SecretVoidFileMeta meta)
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