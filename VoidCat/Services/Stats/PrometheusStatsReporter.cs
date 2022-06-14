using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Stats;

/// <summary>
/// Fetch stats from Prometheus
/// </summary>
public class PrometheusStatsReporter : ITimeSeriesStatsReporter
{
    private readonly ILogger<PrometheusStatsReporter> _logger;
    private readonly HttpClient _client;

    public PrometheusStatsReporter(ILogger<PrometheusStatsReporter> logger, HttpClient client, VoidSettings settings)
    {
        _client = client;
        _logger = logger;

        _client.BaseAddress = settings.Prometheus;
    }

    public async ValueTask<IReadOnlyList<BandwidthPoint>> GetBandwidth(DateTime start, DateTime end)
    {
        var q = "increase(egress{file=\"\"}[1d])";
        return await QueryInner(q, start, end);
    }

    public async ValueTask<IReadOnlyList<BandwidthPoint>> GetBandwidth(Guid id, DateTime start, DateTime end)
    {
        var q = $"increase(egress{{file=\"{id}\"}}[1d])";
        return await QueryInner(q, start, end);
    }

    private async Task<IReadOnlyList<BandwidthPoint>> QueryInner(string query, DateTime start, DateTime end)
    {
        var res = await QueryRange(query, start, end, TimeSpan.FromHours(24));

        var bp = new List<BandwidthPoint>();
        foreach (var r in res.Data.Result)
        {
            foreach (var v in r.Values)
            {
                bp.Add(new(DateTimeOffset.FromUnixTimeSeconds((long) v[0])
                        .DateTime, 0ul,
                    (ulong) decimal.Parse(v[1] as string ?? "0")));
            }
        }

        return bp;
    }

    private async Task<Metrics?> QueryRange(string query, DateTimeOffset start, DateTimeOffset end, TimeSpan step)
    {
        var url =
            $"/api/v1/query_range?query={Uri.EscapeDataString(query)}&start={start.ToUnixTimeSeconds()}&end={end.ToUnixTimeSeconds()}&step={(int) step.TotalSeconds}";
        var req = await _client.SendAsync(new(HttpMethod.Get, url));
        if (req.IsSuccessStatusCode)
        {
            var json = await req.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(json))
            {
                return JsonConvert.DeserializeObject<Metrics>(json);
            }
        }
        else
        {
            _logger.LogWarning("Failed to fetch metrics: {Url} {Status}", url, req.StatusCode);
        }

        return default;
    }

    private class Metrics
    {
        [JsonProperty("status")] public string Status { get; set; }

        [JsonProperty("data")] public MetricData Data { get; set; }

        public class MetricData
        {
            [JsonProperty("resultType")] public string ResultType { get; set; }

            [JsonProperty("result")] public List<Result> Result { get; set; }
        }

        public class Metric
        {
            [JsonProperty("file")] public string File { get; set; }

            [JsonProperty("instance")] public string Instance { get; set; }

            [JsonProperty("job")] public string Job { get; set; }
        }

        public class Result
        {
            [JsonProperty("metric")] public Metric Metric { get; set; }

            [JsonProperty("values")] public List<List<object>> Values { get; set; }
        }
    }
}