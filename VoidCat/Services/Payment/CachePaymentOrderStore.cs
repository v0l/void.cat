using VoidCat.Database;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Payment;

/// <inheritdoc cref="IPaymentOrderStore"/>
public class CachePaymentOrderStore : BasicCacheStore<PaywallOrder>, IPaymentOrderStore
{
    public CachePaymentOrderStore(ICache cache) : base(cache)
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
    protected override string MapKey(Guid id) => $"payment:order:{id}";
}