using Microsoft.Extensions.Caching.Memory;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.InMemory;

public class InMemoryCache : ICache
{
    private readonly IMemoryCache _cache;

    public InMemoryCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public ValueTask<T?> Get<T>(string key)
    {
        return ValueTask.FromResult(_cache.Get<T?>(key));
    }
    
    public ValueTask Set<T>(string key, T value, TimeSpan? expire = null)
    {
        if (expire.HasValue)
        {
            _cache.Set(key, value, expire.Value);
        }
        else
        {
            _cache.Set(key, value);
        }

        return ValueTask.CompletedTask;
    }
    
    public ValueTask<string[]> GetList(string key)
    {
        return ValueTask.FromResult(_cache.Get<string[]>(key));
    }
    
    public ValueTask AddToList(string key, string value)
    {
        var list = new HashSet<string>(GetList(key).Result);
        list.Add(value);
        _cache.Set(key, list.ToArray());
        return ValueTask.CompletedTask;
    }

    public ValueTask Delete(string key)
    {
        _cache.Remove(key);
        return ValueTask.CompletedTask;;
    }
}
