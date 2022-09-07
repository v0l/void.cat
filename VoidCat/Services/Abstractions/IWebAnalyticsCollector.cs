namespace VoidCat.Services.Abstractions;

public interface IWebAnalyticsCollector
{
    Task TrackPageView(HttpContext context);
}