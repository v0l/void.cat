using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Paywall;

public class PaywallStore : IPaywallStore
{
    private readonly ICache _cache;

    public PaywallStore(ICache database)
    {
        _cache = database;
    }

    public async ValueTask<PaywallConfig?> GetConfig(Guid id)
    {
        var cfg = await _cache.Get<NoPaywallConfig>(ConfigKey(id));
        return cfg?.Service switch
        {
            PaywallServices.None => cfg,
            PaywallServices.Strike => await _cache.Get<StrikePaywallConfig>(ConfigKey(id)),
            _ => default
        };
    }

    public async ValueTask SetConfig(Guid id, PaywallConfig config)
    {
        await _cache.Set(ConfigKey(id), config);
    }

    public async ValueTask<PaywallOrder?> GetOrder(Guid id)
    {
        return await _cache.Get<PaywallOrder>(OrderKey(id));
    }

    public async ValueTask SaveOrder(PaywallOrder order)
    {
        await _cache.Set(OrderKey(order.Id), order,
            order.Status == PaywallOrderStatus.Paid ? TimeSpan.FromDays(1) : TimeSpan.FromSeconds(5));
    }

    private string ConfigKey(Guid id) => $"paywall:config:{id}";
    private string OrderKey(Guid id) => $"paywall:order:{id}";
}
