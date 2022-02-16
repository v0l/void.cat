﻿using Prometheus;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services;

public class PrometheusStatsCollector : IStatsCollector
{
    private readonly Counter _egress =
        Metrics.CreateCounter("egress", "Outgoing traffic from the site", "file");

    private readonly Counter _ingress =
        Metrics.CreateCounter("ingress", "Incoming traffic to the site", "file");

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
}