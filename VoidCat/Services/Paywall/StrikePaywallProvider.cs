using System.Globalization;
using VoidCat.Model;
using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Paywall;

public class StrikePaywallProvider : IPaywallProvider
{
    private readonly ILogger<StrikePaywallProvider> _logger;
    private readonly StrikeApi _strike;
    private readonly IPaywallStore _store;

    public StrikePaywallProvider(ILogger<StrikePaywallProvider> logger, StrikeApi strike, IPaywallStore store)
    {
        _logger = logger;
        _strike = strike;
        _store = store;
    }

    public async ValueTask<PaywallOrder?> CreateOrder(PublicVoidFile file)
    {
        IsStrikePaywall(file.Paywall, out var strikeConfig);
        var config = file.Paywall!;

        _logger.LogInformation("Generating invoice for {Currency} {Amount}", config.Cost.Currency, config.Cost.Amount);

        var currency = MapCurrency(strikeConfig!.Cost.Currency);
        if (currency == Currencies.USD)
        {
            // map USD to USDT if USD is not available and USDT is
            var profile = await _strike.GetProfile(strikeConfig!.Handle);
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
            Description = file.Metadata?.Name
        });
        if (invoice != default)
        {
            var quote = await _strike.GetInvoiceQuote(invoice.InvoiceId);
            if (quote != default)
            {
                var order = new LightningPaywallOrder(invoice.InvoiceId, config.Cost, PaywallOrderStatus.Unpaid,
                    quote.LnInvoice!,
                    quote.Expiration);
                await _store.SaveOrder(order);
                return order;
            }

            _logger.LogWarning("Failed to get quote for invoice: {Id}", invoice.InvoiceId);
        }

        _logger.LogWarning("Failed to get invoice for config: {Config}", config);
        return default;
    }

    public async ValueTask<PaywallOrder?> GetOrderStatus(Guid id)
    {
        var order = await _store.GetOrder(id);
        if (order == default)
        {
            var invoice = await _strike.GetInvoice(id);
            if (invoice != default)
            {
                order = new(id, new(decimal.Parse(invoice.Amount!.Amount!), MapCurrency(invoice.Amount.Currency)),
                    MapStatus(invoice.State));
                await _store.SaveOrder(order);
            }
        }

        return order;
    }

    private static Currencies MapCurrency(PaywallCurrencies c)
        => c switch
        {
            PaywallCurrencies.BTC => Currencies.BTC,
            PaywallCurrencies.USD => Currencies.USD,
            PaywallCurrencies.EUR => Currencies.EUR,
            PaywallCurrencies.GBP => Currencies.GBP,
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };

    private static PaywallCurrencies MapCurrency(Currencies? c)
        => c switch
        {
            Currencies.BTC => PaywallCurrencies.BTC,
            Currencies.USD => PaywallCurrencies.USD,
            Currencies.EUR => PaywallCurrencies.EUR,
            Currencies.GBP => PaywallCurrencies.GBP,
            Currencies.USDT => PaywallCurrencies.USD,
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };

    private static PaywallOrderStatus MapStatus(InvoiceState s)
        => s switch
        {
            InvoiceState.UNPAID => PaywallOrderStatus.Unpaid,
            InvoiceState.PENDING => PaywallOrderStatus.Unpaid,
            InvoiceState.PAID => PaywallOrderStatus.Paid,
            InvoiceState.CANCELLED => PaywallOrderStatus.Expired,
            _ => throw new ArgumentOutOfRangeException(nameof(s), s, null)
        };

    private static void IsStrikePaywall(PaywallConfig? cfg, out StrikePaywallConfig? strikeConfig)
    {
        if (cfg?.Service != PaywallServices.Strike)
        {
            throw new ArgumentException("Must be strike paywall");
        }

        strikeConfig = cfg as StrikePaywallConfig;
    }
}