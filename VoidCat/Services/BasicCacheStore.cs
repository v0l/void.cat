﻿using VoidCat.Services.Abstractions;

namespace VoidCat.Services;

/// <inheritdoc />
public abstract class BasicCacheStore<TStore> : IBasicStore<TStore>
{
    protected readonly ICache Cache;

    protected BasicCacheStore(ICache cache)
    {
        Cache = cache;
    }

    /// <inheritdoc />
    public virtual ValueTask<TStore?> Get(Guid id)
    {
        return Cache.Get<TStore>(MapKey(id));
    }

    /// <inheritdoc />
    public virtual ValueTask Add(Guid id, TStore obj)
    {
        return Cache.Set(MapKey(id), obj);
    }

    /// <inheritdoc />
    public virtual ValueTask Delete(Guid id)
    {
        return Cache.Delete(MapKey(id));
    }

    /// <summary>
    /// Map an id to a key in the KV store
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    protected abstract string MapKey(Guid id);
}