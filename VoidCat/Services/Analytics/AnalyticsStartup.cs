using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Analytics;

public static class AnalyticsStartup
{
    /// <summary>
    /// Add services needed to collect analytics
    /// </summary>
    /// <param name="services"></param>
    /// <param name="settings"></param>
    public static void AddAnalytics(this IServiceCollection services, VoidSettings settings)
    {
        services.AddTransient<AnalyticsMiddleware>();
        if (settings.HasPlausible())
        {
            services.AddTransient<IWebAnalyticsCollector, PlausibleAnalytics>();
        }
    }
}