using VoidCat.Model.Payments;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Payment;

/// <inheritdoc cref="IPaymentOrderStore"/>
public class CachePaymentOrderStore : BasicCacheStore<PaymentOrder>, IPaymentOrderStore
{
    public CachePaymentOrderStore(ICache cache) : base(cache)
    {
    }

    /// <inheritdoc />
    public async ValueTask UpdateStatus(Guid order, PaymentOrderStatus status)
    {
        var old = await Get(order);
        if (old == default) return;

        old.Status = status;

        await Add(order, old);
    }

    /// <inheritdoc />
    protected override string MapKey(Guid id) => $"payment:order:{id}";
}