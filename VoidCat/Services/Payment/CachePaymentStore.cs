using VoidCat.Database;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Payment;

/// <inheritdoc cref="IPaymentStore"/>
public class CachePaymentStore : BasicCacheStore<Paywall>, IPaymentStore
{
    public CachePaymentStore(ICache database)
        : base(database)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<Paywall?> Get(Guid id)
    {
        var cfg = await Cache.Get<Paywall>(MapKey(id));
        return cfg;
    }

    /// <inheritdoc />
    protected override string MapKey(Guid id) => $"payment:config:{id}";
}