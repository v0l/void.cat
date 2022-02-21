using Newtonsoft.Json;
using StackExchange.Redis;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Redis;

public class RedisCache : ICache
{
    private readonly IDatabase _db;

    public RedisCache(IDatabase db)
    {
        _db = db;
    }
    
    public async ValueTask<T?> Get<T>(string key)
    {
        var json = await _db.StringGetAsync(key);
        return json.HasValue ? JsonConvert.DeserializeObject<T>(json) : default;
    }
    
    public async ValueTask Set<T>(string key, T value, TimeSpan? expire = null)
    {
        var json = JsonConvert.SerializeObject(value);
        await _db.StringSetAsync(key, json, expire);
    }
    
    public async ValueTask<string[]> GetList(string key)
    {
        return (await _db.SetMembersAsync(key)).ToStringArray();
    }
    
    public async ValueTask AddToList(string key, string value)
    {
        await _db.SetAddAsync(key, value);
    }
}
