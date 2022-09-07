using Dapper;
using VoidCat.Model.Payments;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Payment;

/// <inheritdoc />
public sealed class PostgresPaymentStore : IPaymentStore
{
    private readonly PostgresConnectionFactory _connection;

    public PostgresPaymentStore(PostgresConnectionFactory connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public async ValueTask<PaymentConfig?> Get(Guid id)
    {
        await using var conn = await _connection.Get();
        var dto = await conn.QuerySingleOrDefaultAsync<DtoPaymentConfig>(
            @"select * from ""Payment"" where ""File"" = :file", new {file = id});
        if (dto != default)
        {
            switch (dto.Service)
            {
                case PaymentServices.Strike:
                {
                    var handle =
                        await conn.ExecuteScalarAsync<string>(
                            @"select ""Handle"" from ""PaymentStrike"" where ""File"" = :file", new {file = id});
                    return new StrikePaymentConfig
                    {
                        Cost = new(dto.Amount, dto.Currency),
                        File = dto.File,
                        Handle = handle,
                        Service = PaymentServices.Strike,
                        Required = dto.Required
                    };
                }
            }
        }

        return default;
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<PaymentConfig>> Get(Guid[] ids)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask Add(Guid id, PaymentConfig obj)
    {
        await using var conn = await _connection.Get();
        await using var txn = await conn.BeginTransactionAsync();
        await conn.ExecuteAsync(
            @"insert into ""Payment""(""File"", ""Service"", ""Amount"", ""Currency"", ""Required"") values(:file, :service, :amount, :currency, :required)
on conflict(""File"") do update set ""Service"" = :service, ""Amount"" = :amount, ""Currency"" = :currency, ""Required"" = :required",
            new
            {
                file = id,
                service = (int)obj.Service,
                amount = obj.Cost.Amount,
                currency = obj.Cost.Currency,
                required = obj.Required
            });

        if (obj is StrikePaymentConfig sc)
        {
            await conn.ExecuteAsync(@"insert into ""PaymentStrike""(""File"", ""Handle"") values(:file, :handle)
on conflict(""File"") do update set ""Handle"" = :handle", new
            {
                file = id,
                handle = sc.Handle
            });
        }

        await txn.CommitAsync();
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(@"delete from ""Payment"" where ""File"" = :file", new {file = id});
    }

    private sealed class DtoPaymentConfig : PaymentConfig
    {
        public PaymentCurrencies Currency { get; init; }
        public decimal Amount { get; init; }
    }
}