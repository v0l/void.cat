using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.VirusScanner;

public class VirusScanStore : BasicCacheStore<VirusScanResult>, IVirusScanStore
{
    public VirusScanStore(ICache cache) : base(cache)
    {
    }

    public override string MapKey(Guid id)
        => $"virus-scan:{id}";
}