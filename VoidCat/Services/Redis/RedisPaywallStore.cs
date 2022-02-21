using Newtonsoft.Json;
using StackExchange.Redis;
using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Redis;

public class RedisPaywallStore : IPaywallStore
{
    private readonly IDatabase _database;

    public RedisPaywallStore(IDatabase database)
    {
        _database = database;
    }

    public async ValueTask<PaywallConfig?> GetConfig(Guid id)
    {
        var json = await _database.StringGetAsync(ConfigKey(id));
        var cfg = JsonConvert.DeserializeObject<PaywallConfig>(json);
        return cfg?.Service switch
        {
            PaywallServices.Strike => JsonConvert.DeserializeObject<StrikePaywallConfig>(json),
            _ => default
        };
    }

    public async ValueTask SetConfig(Guid id, PaywallConfig config)
    {
        await _database.StringSetAsync(ConfigKey(id), JsonConvert.SerializeObject(config));
    }

    public async ValueTask<PaywallOrder?> GetOrder(Guid id)
    {
        var json = await _database.StringGetAsync(OrderKey(id));
        return JsonConvert.DeserializeObject<PaywallOrder>(json);
    }

    public async ValueTask SaveOrder(PaywallOrder order)
    {
        await _database.StringSetAsync(OrderKey(order.Id), JsonConvert.SerializeObject(order));
    }

    private RedisKey ConfigKey(Guid id) => $"paywall:config:{id}";
    private RedisKey OrderKey(Guid id) => $"paywall:order:{id}";
}
