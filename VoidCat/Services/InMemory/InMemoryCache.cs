using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.InMemory;

/// <inheritdoc />
public class InMemoryCache : ICache
{
    private readonly IMemoryCache _cache;

    public InMemoryCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public ValueTask<T?> Get<T>(string key)
    {
        var json = _cache.Get<string>(key);
        if (string.IsNullOrEmpty(json)) return default;
        
        return ValueTask.FromResult(JsonConvert.DeserializeObject<T?>(json));
    }

    /// <inheritdoc />
    public ValueTask Set<T>(string key, T value, TimeSpan? expire = null)
    {
        var json = JsonConvert.SerializeObject(value);
        if (expire.HasValue)
        {
            _cache.Set(key, json, expire.Value);
        }
        else
        {
            _cache.Set(key, json);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<string[]> GetList(string key)
    {
        return ValueTask.FromResult(_cache.Get<string[]>(key) ?? Array.Empty<string>());
    }

    /// <inheritdoc />
    public ValueTask AddToList(string key, string value)
    {
        var list = new HashSet<string>(GetList(key).Result);
        list.Add(value);
        _cache.Set(key, list.ToArray());
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask RemoveFromList(string key, string value)
    {
        var list = new HashSet<string>(GetList(key).Result);
        list.Remove(value);
        _cache.Set(key, list.ToArray());
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask Delete(string key)
    {
        _cache.Remove(key);
        return ValueTask.CompletedTask;
    }
}