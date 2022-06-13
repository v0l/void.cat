using Dapper;
using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Paywall;

/// <inheritdoc />
public sealed class PostgresPaywallStore : IPaywallStore
{
    private readonly PostgresConnectionFactory _connection;

    public PostgresPaywallStore(PostgresConnectionFactory connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public async ValueTask<PaywallConfig?> Get(Guid id)
    {
        await using var conn = await _connection.Get();
        var svc = await conn.QuerySingleOrDefaultAsync<DtoPaywallConfig>(
            @"select * from ""Paywall"" where ""File"" = :file", new {file = id});
        if (svc != default)
        {
            switch (svc.Service)
            {
                case PaymentServices.Strike:
                {
                    var handle =
                        await conn.ExecuteScalarAsync<string>(
                            @"select ""Handle"" from ""PaywallStrike"" where ""File"" = :file", new {file = id});
                    return new StrikePaywallConfig
                    {
                        Cost = new(svc.Amount, svc.Currency),
                        File = svc.File,
                        Handle = handle,
                        Service = PaymentServices.Strike
                    };
                }
            }
        }

        return default;
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<PaywallConfig>> Get(Guid[] ids)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask Add(Guid id, PaywallConfig obj)
    {
        await using var conn = await _connection.Get();
        await using var txn = await conn.BeginTransactionAsync();
        await conn.ExecuteAsync(
            @"insert into ""Paywall""(""File"", ""Service"", ""Amount"", ""Currency"") values(:file, :service, :amount, :currency)
on conflict(""File"") do update set ""Service"" = :service, ""Amount"" = :amount, ""Currency"" = :currency",
            new
            {
                file = id,
                service = (int)obj.Service,
                amount = obj.Cost.Amount,
                currency = obj.Cost.Currency
            });

        if (obj is StrikePaywallConfig sc)
        {
            await conn.ExecuteAsync(@"insert into ""PaywallStrike""(""File"", ""Handle"") values(:file, :handle)
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
        await conn.ExecuteAsync(@"delete from ""Paywall"" where ""File"" = :file", new {file = id});
    }

    private sealed class DtoPaywallConfig : PaywallConfig
    {
        public PaywallCurrencies Currency { get; init; }
        public decimal Amount { get; init; }
    }
}