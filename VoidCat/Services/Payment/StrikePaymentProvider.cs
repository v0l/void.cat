using System.Globalization;
using VoidCat.Database;
using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Strike;

namespace VoidCat.Services.Payment;

/// <inheritdoc />
public class StrikePaymentProvider : IPaymentProvider
{
    private readonly ILogger<StrikePaymentProvider> _logger;
    private readonly StrikeApi _strike;
    private readonly IPaymentOrderStore _orderStore;

    public StrikePaymentProvider(ILogger<StrikePaymentProvider> logger, StrikeApi strike, IPaymentOrderStore orderStore)
    {
        _logger = logger;
        _strike = strike;
        _orderStore = orderStore;
    }

    /// <inheritdoc />
    public async ValueTask<PaywallOrder?> CreateOrder(Paywall config)
    {
        if (config.Service != PaywallService.Strike || config.PaywallStrike == default)
        {
            throw new InvalidOperationException("Paywall config is not Strike");
        }
        
        _logger.LogInformation("Generating invoice for {Currency} {Amount}", config.Currency, config.Amount);

        var currency = MapCurrency(config.Currency);
        if (currency == Currencies.USD)
        {
            // map USD to USDT if USD is not available and USDT is
            var profile = await _strike.GetProfile(config.PaywallStrike!.Handle);
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
            Handle = config.PaywallStrike.Handle,
            Amount = new()
            {
                Amount = config.Amount.ToString(CultureInfo.InvariantCulture),
                Currency = currency
            },
            Description = config.File.Id.ToBase58()
        });
        if (invoice != default)
        {
            var quote = await _strike.GetInvoiceQuote(invoice.InvoiceId);
            if (quote != default)
            {
                var order = new PaywallOrder
                {
                    Id = invoice.InvoiceId,
                    FileId = config.File.Id,
                    Service = PaywallService.Strike,
                    Amount = config.Amount,
                    Status = PaywallOrderStatus.Unpaid,
                    OrderLightning = new()
                    {
                        OrderId = invoice.InvoiceId,
                        Invoice = quote.LnInvoice!,
                        Expire = DateTime.SpecifyKind(quote.Expiration.DateTime, DateTimeKind.Utc)
                    }
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
                Amount = decimal.Parse(providerOrder.Amount!.Amount!),
                Currency = MapCurrency(providerOrder.Amount!.Currency!.Value),
                Service = PaywallService.Strike,
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

    private static Currencies MapCurrency(PaywallCurrency c)
        => c switch
        {
            PaywallCurrency.BTC => Currencies.BTC,
            PaywallCurrency.USD => Currencies.USD,
            PaywallCurrency.EUR => Currencies.EUR,
            PaywallCurrency.GBP => Currencies.GBP,
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };

    private static PaywallCurrency MapCurrency(Currencies c)
        => c switch
        {
            Currencies.BTC => PaywallCurrency.BTC,
            Currencies.USD => PaywallCurrency.USD,
            Currencies.USDT => PaywallCurrency.USD,
            Currencies.EUR => PaywallCurrency.EUR,
            Currencies.GBP => PaywallCurrency.GBP,
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };
}