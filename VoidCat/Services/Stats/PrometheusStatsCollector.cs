using Prometheus;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Stats;

/// <inheritdoc />
public class PrometheusStatsCollector : IStatsCollector
{
    private readonly Counter _egress =
        Metrics.CreateCounter("egress", "Outgoing traffic from the site", "file");

    private readonly Counter _ingress =
        Metrics.CreateCounter("ingress", "Incoming traffic to the site", "file");

    /// <inheritdoc />
    public ValueTask TrackIngress(Guid id, ulong amount)
    {
        _ingress.Inc(amount);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask TrackEgress(Guid id, ulong amount)
    {
        _egress.Inc(amount);
        return ValueTask.CompletedTask;
    }
}