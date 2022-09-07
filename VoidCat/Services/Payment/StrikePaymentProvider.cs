using System.Globalization;
using VoidCat.Model;
using VoidCat.Model.Payments;
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
    public async ValueTask<PaymentOrder?> CreateOrder(PaymentConfig config)
    {
        IsStrikePayment(config, out var strikeConfig);
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
                var order = new LightningPaymentOrder
                {
                    Id = invoice.InvoiceId,
                    File = config.File,
                    Service = PaymentServices.Strike,
                    Price = config.Cost,
                    Status = PaymentOrderStatus.Unpaid,
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
    public async ValueTask<PaymentOrder?> GetOrderStatus(Guid id)
    {
        var order = await _orderStore.Get(id);
        if (order is {Status: PaymentOrderStatus.Paid or PaymentOrderStatus.Expired}) return order;

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

    private PaymentOrderStatus MapStatus(InvoiceState providerOrderState)
        => providerOrderState switch
        {
            InvoiceState.UNPAID => PaymentOrderStatus.Unpaid,
            InvoiceState.PENDING => PaymentOrderStatus.Unpaid,
            InvoiceState.PAID => PaymentOrderStatus.Paid,
            InvoiceState.CANCELLED => PaymentOrderStatus.Expired,
            _ => throw new ArgumentOutOfRangeException(nameof(providerOrderState), providerOrderState, null)
        };

    private static Currencies MapCurrency(PaymentCurrencies c)
        => c switch
        {
            PaymentCurrencies.BTC => Currencies.BTC,
            PaymentCurrencies.USD => Currencies.USD,
            PaymentCurrencies.EUR => Currencies.EUR,
            PaymentCurrencies.GBP => Currencies.GBP,
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };

    private static PaymentCurrencies MapCurrency(Currencies c)
        => c switch
        {
            Currencies.BTC => PaymentCurrencies.BTC,
            Currencies.USD => PaymentCurrencies.USD,
            Currencies.USDT => PaymentCurrencies.USD,
            Currencies.EUR => PaymentCurrencies.EUR,
            Currencies.GBP => PaymentCurrencies.GBP,
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };

    private static void IsStrikePayment(PaymentConfig? cfg, out StrikePaymentConfig strikeConfig)
    {
        if (cfg?.Service != PaymentServices.Strike)
        {
            throw new ArgumentException("Must be strike Payment");
        }

        strikeConfig = (cfg as StrikePaymentConfig)!;
    }
}