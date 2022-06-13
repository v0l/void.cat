using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Paywall;

/// <inheritdoc cref="IPaywallOrderStore"/>
public class CachePaywallOrderStore : BasicCacheStore<PaywallOrder>, IPaywallOrderStore
{
    public CachePaywallOrderStore(ICache cache) : base(cache)
    {
    }

    /// <inheritdoc />
    public async ValueTask UpdateStatus(Guid order, PaywallOrderStatus status)
    {
        var old = await Get(order);
        if (old == default) return;

        old.Status = status;

        await Add(order, old);
    }

    /// <inheritdoc />
    protected override string MapKey(Guid id) => $"paywall:order:{id}";
}