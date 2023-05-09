using VoidCat.Database;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Payment;

public class PaymentFactory : IPaymentFactory
{
    private readonly IServiceProvider _services;

    public PaymentFactory(IServiceProvider services)
    {
        _services = services;
    }

    public ValueTask<IPaymentProvider> CreateProvider(PaywallService svc)
    {
        return ValueTask.FromResult<IPaymentProvider>(svc switch
        {
            PaywallService.Strike => _services.GetRequiredService<StrikePaymentProvider>(),
            _ => throw new ArgumentException("Must have a payment config", nameof(svc))
        });
    }
}