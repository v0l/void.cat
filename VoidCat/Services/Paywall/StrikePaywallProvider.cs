using System.Globalization;
using VoidCat.Model;
using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Strike;

namespace VoidCat.Services.Paywall;

/// <inheritdoc />
public class StrikePaywallProvider : IPaywallProvider
{
    private readonly ILogger<StrikePaywallProvider> _logger;
    private readonly StrikeApi _strike;
    private readonly IPaywallOrderStore _orderStore;

    public StrikePaywallProvider(ILogger<StrikePaywallProvider> logger, StrikeApi strike, IPaywallOrderStore orderStore)
    {
        _logger = logger;
        _strike = strike;
        _orderStore = orderStore;
    }

    /// <inheritdoc />
    public async ValueTask<PaywallOrder?> CreateOrder(PaywallConfig config)
    {
        IsStrikePaywall(config, out var strikeConfig);
        _logger.LogInformation("Generating invoice for {Currency} {Amount}", config.Cost.Currency, config.Cost.Amount);

        var currency = MapCurrency(strikeConfig.Cost.Currency);
        if (currency == Currencies.USD)
        {
            // map USD to USDT if USD is not available and USDT is
            var profile = await _strike.GetProfile(strikeConfig.Handle);
            if (profile != default)
            {
                var usd = profile.Currencies.FirstOrDefault(a => a.Currency == Currencies.USD);
                var usdt = profile.Currencies.FirstOrDefault(a => a.Currency == Currencies.USDT);
                if (!(usd?.IsAvailable ?? false) && (usdt?.IsAvailable ?? false))
                {
                    currency = Currencies.USDT;
                }
            }
        }

        var invoice = await _strike.GenerateInvoice(new()
        {
            Handle = strikeConfig.Handle,
            Amount = new()
            {
                Amount = strikeConfig.Cost.Amount.ToString(CultureInfo.InvariantCulture),
                Currency = currency
            },
            Description = config.File.ToBase58()
        });
        if (invoice != default)
        {
            var quote = await _strike.GetInvoiceQuote(invoice.InvoiceId);
            if (quote != default)
            {
                var order = new LightningPaywallOrder
                {
                    Id = invoice.InvoiceId,
                    File = config.File,
                    Service = PaymentServices.Strike,
                    Price = config.Cost,
                    Status = PaywallOrderStatus.Unpaid,
                    Invoice = quote.LnInvoice!,
                    Expire = DateTime.SpecifyKind(quote.Expiration.DateTime, DateTimeKind.Utc)
                };
                await _orderStore.Add(order.Id, order);
                return order;
            }

            _logger.LogWarning("Failed to get quote for invoice: {Id}", invoice.InvoiceId);
        }

        _logger.LogWarning("Failed to get invoice for config: File={File}, Service={Service}", config.File,
            config.Service.ToString());
        return default;
    }

    /// <inheritdoc />
    public async ValueTask<PaywallOrder?> GetOrderStatus(Guid id)
    {
        var order = await _orderStore.Get(id);
        if (order is {Status: PaywallOrderStatus.Paid or PaywallOrderStatus.Expired}) return order;

        var providerOrder = await _strike.GetInvoice(id);
        if (providerOrder != default)
        {
            var status = MapStatus(providerOrder.State);
            await _orderStore.UpdateStatus(id, status);

            return new()
            {
                Id = id,
                Price = new(decimal.Parse(providerOrder!.Amount!.Amount!),
                    MapCurrency(providerOrder.Amount!.Currency!.Value)),
                Service = PaymentServices.Strike,
                Status = status
            };
        }

        return default;
    }

    private PaywallOrderStatus MapStatus(InvoiceState providerOrderState)
        => providerOrderState switch
        {
            InvoiceState.UNPAID => PaywallOrderStatus.Unpaid,
            InvoiceState.PENDING => PaywallOrderStatus.Unpaid,
            InvoiceState.PAID => PaywallOrderStatus.Paid,
            InvoiceState.CANCELLED => PaywallOrderStatus.Expired,
            _ => throw new ArgumentOutOfRangeException(nameof(providerOrderState), providerOrderState, null)
        };

    private static Currencies MapCurrency(PaywallCurrencies c)
        => c switch
        {
            PaywallCurrencies.BTC => Currencies.BTC,
            PaywallCurrencies.USD => Currencies.USD,
            PaywallCurrencies.EUR => Currencies.EUR,
            PaywallCurrencies.GBP => Currencies.GBP,
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };

    private static PaywallCurrencies MapCurrency(Currencies c)
        => c switch
        {
            Currencies.BTC => PaywallCurrencies.BTC,
            Currencies.USD => PaywallCurrencies.USD,
            Currencies.USDT => PaywallCurrencies.USD,
            Currencies.EUR => PaywallCurrencies.EUR,
            Currencies.GBP => PaywallCurrencies.GBP,
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };

    private static void IsStrikePaywall(PaywallConfig? cfg, out StrikePaywallConfig strikeConfig)
    {
        if (cfg?.Service != PaymentServices.Strike)
        {
            throw new ArgumentException("Must be strike paywall");
        }

        strikeConfig = (cfg as StrikePaywallConfig)!;
    }
}