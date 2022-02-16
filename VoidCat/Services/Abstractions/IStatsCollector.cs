namespace VoidCat.Services.Abstractions;

public interface IStatsCollector
{
    ValueTask TrackIngress(Guid id, ulong amount);
    ValueTask TrackEgress(Guid id, ulong amount);

    ValueTask<Bandwidth> GetBandwidth();
    ValueTask<Bandwidth> GetBandwidth(Guid id);
}

public sealed record Bandwidth(ulong Ingress, ulong Egress);