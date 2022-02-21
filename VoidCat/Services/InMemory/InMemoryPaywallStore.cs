using Microsoft.Extensions.Caching.Memory;
using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.InMemory;

public class InMemoryPaywallStore : IPaywallStore
{
    private readonly IMemoryCache _cache;

    public InMemoryPaywallStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public ValueTask<PaywallConfig?> GetConfig(Guid id)
    {
        return ValueTask.FromResult(_cache.Get(id) as PaywallConfig);
    }

    public ValueTask SetConfig(Guid id, PaywallConfig config)
    {
        _cache.Set(id, config);
        return ValueTask.CompletedTask;
    }

    public ValueTask<PaywallOrder?> GetOrder(Guid id)
    {
        return ValueTask.FromResult(_cache.Get(id) as PaywallOrder);
    }

    public ValueTask SaveOrder(PaywallOrder order)
    {
        _cache.Set(order.Id, order,
            order.Status == PaywallOrderStatus.Paid ? TimeSpan.FromDays(1) : TimeSpan.FromSeconds(5));
        return ValueTask.CompletedTask;
    }
}