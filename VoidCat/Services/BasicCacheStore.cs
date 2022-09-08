using VoidCat.Services.Abstractions;

namespace VoidCat.Services;

/// <inheritdoc />
public abstract class BasicCacheStore<TStore> : IBasicStore<TStore>
{
    protected readonly ICache _cache;

    protected BasicCacheStore(ICache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public virtual ValueTask<TStore?> Get(Guid id)
    {
        return _cache.Get<TStore>(MapKey(id));
    }

    /// <inheritdoc />
    public virtual ValueTask Add(Guid id, TStore obj)
    {
        return _cache.Set(MapKey(id), obj);
    }

    /// <inheritdoc />
    public virtual ValueTask Delete(Guid id)
    {
        return _cache.Delete(MapKey(id));
    }

    /// <summary>
    /// Map an id to a key in the KV store
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    protected abstract string MapKey(Guid id);
}