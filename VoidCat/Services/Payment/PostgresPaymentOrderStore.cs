using Dapper;
using VoidCat.Model.Payments;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Payment;

/// <inheritdoc />
public class PostgresPaymentOrderStore : IPaymentOrderStore
{
    private readonly PostgresConnectionFactory _connection;

    public PostgresPaymentOrderStore(PostgresConnectionFactory connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public async ValueTask<PaymentOrder?> Get(Guid id)
    {
        await using var conn = await _connection.Get();
        var order = await conn.QuerySingleOrDefaultAsync<DtoPaymentOrder>(
            @"select * from ""PaymentOrder"" where ""Id"" = :id", new {id});
        if (order.Service is PaymentServices.Strike)
        {
            var lnDetails = await conn.QuerySingleAsync<LightningPaymentOrder>(
                @"select * from ""PaymentOrderLightning"" where ""Order"" = :id", new
                {
                    id = order.Id
                });
            return new LightningPaymentOrder
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
    public ValueTask<IReadOnlyList<PaymentOrder>> Get(Guid[] ids)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask Add(Guid id, PaymentOrder obj)
    {
        await using var conn = await _connection.Get();
        await using var txn = await conn.BeginTransactionAsync();
        await conn.ExecuteAsync(
            @"insert into ""PaymentOrder""(""Id"", ""File"", ""Service"", ""Currency"", ""Amount"", ""Status"") 
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

        if (obj is LightningPaymentOrder ln)
        {
            await conn.ExecuteAsync(
                @"insert into ""PaymentOrderLightning""(""Order"", ""Invoice"", ""Expire"") values(:order, :invoice, :expire)",
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
        await conn.ExecuteAsync(@"delete from ""PaymentOrder"" where ""Id"" = :id", new {id});
    }

    /// <inheritdoc />
    public async ValueTask UpdateStatus(Guid order, PaymentOrderStatus status)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(@"update ""PaymentOrder"" set ""Status"" = :status where ""Id"" = :id",
            new {id = order, status = (int) status});
    }

    private sealed class DtoPaymentOrder : PaymentOrder
    {
        public PaymentCurrencies Currency { get; init; }
        public decimal Amount { get; init; }
    }
}