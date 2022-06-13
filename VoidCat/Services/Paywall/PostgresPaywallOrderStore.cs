using Dapper;
using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Paywall;

/// <inheritdoc />
public class PostgresPaywallOrderStore : IPaywallOrderStore
{
    private readonly PostgresConnectionFactory _connection;

    public PostgresPaywallOrderStore(PostgresConnectionFactory connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public async ValueTask<PaywallOrder?> Get(Guid id)
    {
        await using var conn = await _connection.Get();
        var order = await conn.QuerySingleOrDefaultAsync<DtoPaywallOrder>(
            @"select * from ""PaywallOrder"" where ""Id"" = :id", new {id});
        if (order.Service is PaymentServices.Strike)
        {
            var lnDetails = await conn.QuerySingleAsync<LightningPaywallOrder>(
                @"select * from ""PaywallOrderLightning"" where ""Order"" = :id", new
                {
                    id = order.Id
                });
            return new LightningPaywallOrder
            {
                Id = order.Id,
                File = order.File,
                Price = new(order.Amount, order.Currency),
                Service = order.Service,
                Status = order.Status,
                Invoice = lnDetails.Invoice,
                Expire = lnDetails.Expire
            };
        }

        return order;
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<PaywallOrder>> Get(Guid[] ids)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask Add(Guid id, PaywallOrder obj)
    {
        await using var conn = await _connection.Get();
        await using var txn = await conn.BeginTransactionAsync();
        await conn.ExecuteAsync(
            @"insert into ""PaywallOrder""(""Id"", ""File"", ""Service"", ""Currency"", ""Amount"", ""Status"") 
values(:id, :file, :service, :currency, :amt, :status)",
            new
            {
                id,
                file = obj.File,
                service = (int) obj.Service,
                currency = (int) obj.Price.Currency,
                amt = obj.Price.Amount, // :amount wasn't working?
                status = (int) obj.Status
            });

        if (obj is LightningPaywallOrder ln)
        {
            await conn.ExecuteAsync(
                @"insert into ""PaywallOrderLightning""(""Order"", ""Invoice"", ""Expire"") values(:order, :invoice, :expire)",
                new
                {
                    order = id,
                    invoice = ln.Invoice,
                    expire = ln.Expire.ToUniversalTime()
                });
        }

        await txn.CommitAsync();
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(@"delete from ""PaywallOrder"" where ""Id"" = :id", new {id});
    }

    /// <inheritdoc />
    public async ValueTask UpdateStatus(Guid order, PaywallOrderStatus status)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(@"update ""PaywallOrder"" set ""Status"" = :status where ""Id"" = :id",
            new {id = order, status = (int) status});
    }

    private sealed class DtoPaywallOrder : PaywallOrder
    {
        public PaywallCurrencies Currency { get; init; }
        public decimal Amount { get; init; }
    }
}