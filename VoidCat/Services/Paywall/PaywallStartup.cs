using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Strike;

namespace VoidCat.Services.Paywall;

public static class PaywallStartup
{
    /// <summary>
    /// Add services required to use paywall functions
    /// </summary>
    /// <param name="services"></param>
    /// <param name="settings"></param>
    public static void AddPaywallServices(this IServiceCollection services, VoidSettings settings)
    {
        services.AddTransient<IPaywallFactory, PaywallFactory>();
        if (settings.HasPostgres())
        {
            services.AddTransient<IPaywallStore, PostgresPaywallStore>();
            services.AddTransient<IPaywallOrderStore, PostgresPaywallOrderStore>();
        }
        else
        {
            services.AddTransient<IPaywallStore, CachePaywallStore>();
            services.AddTransient<IPaywallOrderStore, CachePaywallOrderStore>();
        }

        // strike
        services.AddTransient<StrikeApi>();
        services.AddTransient<StrikePaywallProvider>();
    }
}