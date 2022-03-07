using VoidCat.Services.Abstractions;

namespace VoidCat.Services;

public abstract class BasicCacheStore<TStore> : IBasicStore<TStore>
{
    protected readonly ICache _cache;

    protected BasicCacheStore(ICache cache)
    {
        _cache = cache;
    }

    public virtual ValueTask<TStore?> Get(Guid id)
    {
        return _cache.Get<TStore>(MapKey(id));
    }

    public virtual ValueTask Set(Guid id, TStore obj)
    {
        return _cache.Set(MapKey(id), obj);
    }

    public virtual ValueTask Delete(Guid id)
    {
        return _cache.Delete(MapKey(id));
    }

    public abstract string MapKey(Guid id);
}