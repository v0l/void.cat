using Microsoft.EntityFrameworkCore;
using VoidCat.Database;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Payment;

/// <inheritdoc />
public sealed class PostgresPaymentStore : IPaymentStore
{
    private readonly VoidContext _db;

    public PostgresPaymentStore(VoidContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async ValueTask<Paywall?> Get(Guid id)
    {
        return await _db.Paywalls
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc />
    public async ValueTask Add(Guid id, Paywall obj)
    {
        _db.Paywalls.Add(obj);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await _db.Paywalls
            .Where(a => a.Id == id)
            .ExecuteDeleteAsync();
    }
}
