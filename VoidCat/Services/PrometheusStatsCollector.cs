using Prometheus;

namespace VoidCat.Services;

public class PrometheusStatsCollector : IStatsCollector
{
    private readonly Counter _egress =
        Metrics.CreateCounter("egress", "Outgoing traffic from the site", new[] {"file"});

    private readonly Counter _ingress =
        Metrics.CreateCounter("ingress", "Incoming traffic to the site", new[] {"file"});

    public ValueTask TrackIngress(Guid id, ulong amount)
    {
        _ingress.Inc(amount);
        _ingress.WithLabels(id.ToString()).Inc(amount);
        return ValueTask.CompletedTask;
    }

    public ValueTask TrackEgress(Guid id, ulong amount)
    {
        _egress.Inc(amount);
        _egress.WithLabels(id.ToString()).Inc(amount);
        return ValueTask.CompletedTask;
    }

    public ValueTask<Bandwidth> GetBandwidth()
    {
        return ValueTask.FromResult<Bandwidth>(new((ulong) _ingress.Value, (ulong) _egress.Value));
    }

    public ValueTask<Bandwidth> GetBandwidth(Guid id)
    {
        return ValueTask.FromResult<Bandwidth>(new((ulong) _ingress.Labels(id.ToString()).Value,
            (ulong) _egress.Labels(id.ToString()).Value));
    }
}