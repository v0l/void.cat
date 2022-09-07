using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Strike;

namespace VoidCat.Services.Payment;

public static class PaymentStartup
{
    /// <summary>
    /// Add services required to use payment functions
    /// </summary>
    /// <param name="services"></param>
    /// <param name="settings"></param>
    public static void AddPaymentServices(this IServiceCollection services, VoidSettings settings)
    {
        services.AddTransient<IPaymentFactory, PaymentFactory>();
        if (settings.HasPostgres())
        {
            services.AddTransient<IPaymentStore, PostgresPaymentStore>();
            services.AddTransient<IPaymentOrderStore, PostgresPaymentOrderStore>();
        }
        else
        {
            services.AddTransient<IPaymentStore, CachePaymentStore>();
            services.AddTransient<IPaymentOrderStore, CachePaymentOrderStore>();
        }

        // strike
        services.AddTransient<StrikeApi>();
        services.AddTransient<StrikePaymentProvider>();
    }
}