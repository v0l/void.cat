using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Analytics;

public class AnalyticsMiddleware : IMiddleware
{
    private readonly ILogger<AnalyticsMiddleware> _logger;
    private readonly IEnumerable<IWebAnalyticsCollector> _collectors;

    public AnalyticsMiddleware(IEnumerable<IWebAnalyticsCollector> collectors, ILogger<AnalyticsMiddleware> logger)
    {
        _collectors = collectors;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        foreach (var collector in _collectors)
        {
            try
            {
                await collector.TrackPageView(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track page view");
            }
        }

        await next(context);
    }
}