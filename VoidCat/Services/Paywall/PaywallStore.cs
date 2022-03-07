using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Paywall;

public class PaywallStore : BasicCacheStore<PaywallConfig>, IPaywallStore
{
    public PaywallStore(ICache database)
        : base(database)
    {
    }

    public override async ValueTask<PaywallConfig?> Get(Guid id)
    {
        var cfg = await _cache.Get<NoPaywallConfig>(MapKey(id));
        return cfg?.Service switch
        {
            PaywallServices.None => cfg,
            PaywallServices.Strike => await _cache.Get<StrikePaywallConfig>(MapKey(id)),
            _ => default
        };
    }

    public async ValueTask<PaywallOrder?> GetOrder(Guid id)
    {
        return await _cache.Get<PaywallOrder>(OrderKey(id));
    }

    public ValueTask SaveOrder(PaywallOrder order)
    {
        return _cache.Set(OrderKey(order.Id), order,
            order.Status == PaywallOrderStatus.Paid ? TimeSpan.FromDays(1) : TimeSpan.FromSeconds(5));
    }

    public override string MapKey(Guid id) => $"paywall:config:{id}";
    private string OrderKey(Guid id) => $"paywall:order:{id}";
}