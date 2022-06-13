using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Paywall;

public class PaywallFactory : IPaywallFactory
{
    private readonly IServiceProvider _services;

    public PaywallFactory(IServiceProvider services)
    {
        _services = services;
    }

    public ValueTask<IPaywallProvider> CreateProvider(PaymentServices svc)
    {
        return ValueTask.FromResult<IPaywallProvider>(svc switch
        {
            PaymentServices.Strike => _services.GetRequiredService<StrikePaywallProvider>(),
            _ => throw new ArgumentException("Must have a paywall config", nameof(svc))
        });
    }
}