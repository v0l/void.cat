using System.Text;
using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Analytics;

public class PlausibleAnalytics : IWebAnalyticsCollector
{
    private readonly HttpClient _client;
    private readonly string _domain;

    public PlausibleAnalytics(HttpClient client, VoidSettings settings)
    {
        _client = client;
        _client.BaseAddress = settings.PlausibleAnalytics!.Endpoint!;
        _domain = settings.PlausibleAnalytics!.Domain!;
    }

    public async Task TrackPageView(HttpContext context)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/event");
        request.Headers.Add("user-agent", context.Request.Headers.UserAgent.First());
        request.Headers.Add("x-forwarded-for",
            context.Request.Headers.TryGetValue("x-forwarded-for", out var xff) ? xff.First() : null);

        var ub = new UriBuilder("http:", context.Request.Host.Host, context.Request.Host.Port ?? 80,
            context.Request.Path)
        {
            Query = context.Request.QueryString.Value
        };

        var ev = new EventObj(_domain, ub.Uri)
        {
            Referrer =
                context.Request.Headers.Referer.Any()
                    ? new Uri(context.Request.Headers.Referer.FirstOrDefault()!)
                    : null
        };
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ev)));
        request.Content.Headers.ContentType = new("application/json");

        var rsp = await _client.SendAsync(request);
        if (!rsp.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Invalid plausible analytics response {rsp.StatusCode} {await rsp.Content.ReadAsStringAsync()}");
        }
    }

    internal class EventObj
    {
        public EventObj(string domain, Uri url)
        {
            Domain = domain;
            Url = url;
        }

        [JsonProperty("name")] public string Name { get; init; } = "pageview";

        [JsonProperty("domain")] public string Domain { get; init; }

        [JsonProperty("url")] public Uri Url { get; init; }

        [JsonProperty("screen_width")] public int? ScreenWidth { get; init; }

        [JsonProperty("referrer")] public Uri? Referrer { get; init; }

        [JsonProperty("props")] public object? Props { get; init; }
    }
}