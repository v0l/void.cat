using VoidCat.Model.Payments;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Payment;

/// <inheritdoc cref="IPaymentStore"/>
public class CachePaymentStore : BasicCacheStore<PaymentConfig>, IPaymentStore
{
    public CachePaymentStore(ICache database)
        : base(database)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<PaymentConfig?> Get(Guid id)
    {
        var cfg = await _cache.Get<NoPaymentConfig>(MapKey(id));
        return cfg?.Service switch
        {
            PaymentServices.None => cfg,
            PaymentServices.Strike => await _cache.Get<StrikePaymentConfig>(MapKey(id)),
            _ => default
        };
    }

    /// <inheritdoc />
    protected override string MapKey(Guid id) => $"payment:config:{id}";
}