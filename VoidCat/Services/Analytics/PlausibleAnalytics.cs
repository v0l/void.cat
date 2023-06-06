using System.Text;
using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Analytics;

public class PlausibleAnalytics : IWebAnalyticsCollector
{
    private readonly HttpClient _client;
    private readonly ILogger<PlausibleAnalytics> _logger;
    private readonly string _domain;
    private readonly Uri _siteUrl;

    public PlausibleAnalytics(HttpClient client, VoidSettings settings, ILogger<PlausibleAnalytics> logger)
    {
        _client = client;
        _logger = logger;
        _client.BaseAddress = settings.PlausibleAnalytics!.Endpoint!;
        _client.Timeout = TimeSpan.FromSeconds(1);
        _domain = settings.PlausibleAnalytics!.Domain!;
        _siteUrl = settings.SiteUrl;
    }

    public async Task TrackPageView(HttpContext context)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/event");
        request.Headers.UserAgent.ParseAdd(context.Request.Headers.UserAgent);
        if (context.Request.Headers.TryGetValue("x-forwarded-for", out var xff))
        {
            foreach (var xf in xff)
            {
                request.Headers.Add("x-forwarded-for", xf);
            }
        }

        var ub = new UriBuilder(_siteUrl)
        {
            Path = context.Request.Path,
            Query = context.Request.QueryString.ToUriComponent()
        };

        var ev = new EventObj(_domain, ub.Uri)
        {
            Referrer =
                context.Request.Headers.Referer.Any()
                    ? new Uri(context.Request.Headers.Referer.ToString())
                    : null
        };

        var json = JsonConvert.SerializeObject(ev, new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        _logger.LogDebug("Sending pageview {request} {json}", request.ToString(), json);
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

        [JsonProperty("name")]
        public string Name { get; init; } = "pageview";

        [JsonProperty("domain")]
        public string Domain { get; init; }

        [JsonProperty("url")]
        public Uri Url { get; init; }

        [JsonProperty("screen_width")]
        public int? ScreenWidth { get; init; }

        [JsonProperty("referrer")]
        public Uri? Referrer { get; init; }

        [JsonProperty("props")]
        public object? Props { get; init; }
    }
}
