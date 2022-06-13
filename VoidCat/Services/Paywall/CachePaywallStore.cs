using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Paywall;

/// <inheritdoc cref="IPaywallStore"/>
public class CachePaywallStore : BasicCacheStore<PaywallConfig>, IPaywallStore
{
    public CachePaywallStore(ICache database)
        : base(database)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<PaywallConfig?> Get(Guid id)
    {
        var cfg = await Cache.Get<NoPaywallConfig>(MapKey(id));
        return cfg?.Service switch
        {
            PaymentServices.None => cfg,
            PaymentServices.Strike => await Cache.Get<StrikePaywallConfig>(MapKey(id)),
            _ => default
        };
    }

    /// <inheritdoc />
    protected override string MapKey(Guid id) => $"paywall:config:{id}";
}