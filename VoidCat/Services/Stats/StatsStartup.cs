using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.InMemory;
using VoidCat.Services.Redis;

namespace VoidCat.Services.Stats;

public static class StatsStartup
{
    public static void AddMetrics(this IServiceCollection services, VoidSettings settings)
    {
        services.AddTransient<IAggregateStatsCollector, AggregateStatsCollector>();
        services.AddTransient<IStatsCollector, PrometheusStatsCollector>();

        if (settings.HasPrometheus())
        {
            services.AddTransient<ITimeSeriesStatsReporter, PrometheusStatsReporter>();
        }
        else
        {
            services.AddTransient<ITimeSeriesStatsReporter, NoTimeSeriesStatsReporter>();
        }

        if (settings.HasRedis())
        {
            services.AddTransient<RedisStatsController>();
            services.AddTransient<IStatsReporter>(svc => svc.GetRequiredService<RedisStatsController>());
            services.AddTransient<IStatsCollector>(svc => svc.GetRequiredService<RedisStatsController>());
        }
        else
        {
            services.AddTransient<InMemoryStatsController>();
            services.AddTransient<IStatsReporter>(svc => svc.GetRequiredService<InMemoryStatsController>());
            services.AddTransient<IStatsCollector>(svc => svc.GetRequiredService<InMemoryStatsController>());
        }
    }
}