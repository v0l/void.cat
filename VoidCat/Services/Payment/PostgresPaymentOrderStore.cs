using Microsoft.EntityFrameworkCore;
using VoidCat.Database;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Payment;

/// <inheritdoc />
public class PostgresPaymentOrderStore : IPaymentOrderStore
{
    private readonly VoidContext _db;

    public PostgresPaymentOrderStore(VoidContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async ValueTask<PaywallOrder?> Get(Guid id)
    {
        return await _db.PaywallOrders
            .AsNoTracking()
            .Include(a => a.OrderLightning)
            .SingleOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc />
    public async ValueTask Add(Guid id, PaywallOrder obj)
    {
        _db.PaywallOrders.Add(obj);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await _db.PaywallOrders
            .Where(a => a.Id == id)
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public async ValueTask UpdateStatus(Guid order, PaywallOrderStatus status)
    {
        await _db.PaywallOrders
            .Where(a => a.Id == order)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, status));
    }
}
