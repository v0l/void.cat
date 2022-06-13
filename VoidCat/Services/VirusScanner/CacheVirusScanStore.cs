using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.VirusScanner;

/// <inheritdoc cref="IVirusScanStore"/>
public class CacheVirusScanStore : BasicCacheStore<VirusScanResult>, IVirusScanStore
{
    public CacheVirusScanStore(ICache cache) : base(cache)
    {
    }

    /// <inheritdoc />
    public override async ValueTask Add(Guid id, VirusScanResult obj)
    {
        await base.Add(id, obj);
        await Cache.AddToList(MapFilesKey(id), obj.Id.ToString());
    }

    /// <inheritdoc />
    public async ValueTask<VirusScanResult?> GetByFile(Guid id)
    {
        var scans = await Cache.GetList(MapFilesKey(id));
        if (scans.Length > 0)
        {
            return await Get(Guid.Parse(scans.First()));
        }

        return default;
    }

    /// <inheritdoc />
    protected override string MapKey(Guid id)
        => $"virus-scan:{id}";

    private string MapFilesKey(Guid id)
        => $"virus-scan:file:{id}";
}