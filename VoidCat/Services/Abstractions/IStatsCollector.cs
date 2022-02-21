namespace VoidCat.Services.Abstractions;

public interface IStatsCollector
{
    ValueTask TrackIngress(Guid id, ulong amount);
    ValueTask TrackEgress(Guid id, ulong amount);
}