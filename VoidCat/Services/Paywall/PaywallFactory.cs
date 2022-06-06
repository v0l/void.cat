using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Strike;

namespace VoidCat.Services.Paywall;

public class PaywallFactory : IPaywallFactory
{
    private readonly IServiceProvider _services;

    public PaywallFactory(IServiceProvider services)
    {
        _services = services;
    }

    public ValueTask<IPaywallProvider> CreateProvider(PaywallServices svc)
    {
        return ValueTask.FromResult<IPaywallProvider>(svc switch
        {
            PaywallServices.Strike => _services.GetRequiredService<StrikePaywallProvider>(),
            _ => throw new ArgumentException("Must have a paywall config", nameof(svc))
        });
    }
}

public static class Paywall
{
    public static void AddVoidPaywall(this IServiceCollection services)
    {
        services.AddTransient<IPaywallFactory, PaywallFactory>();
        services.AddTransient<IPaywallStore, PaywallStore>();

        // strike
        services.AddTransient<StrikeApi>();
        services.AddTransient<StrikePaywallProvider>();
        services.AddTransient<IPaywallProvider>((svc) => svc.GetRequiredService<StrikePaywallProvider>());
    }
}