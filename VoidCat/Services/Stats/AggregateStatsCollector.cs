using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Stats;

public class AggregateStatsCollector : IAggregateStatsCollector
{
    private readonly IEnumerable<IStatsCollector> _collectors;

    public AggregateStatsCollector(IEnumerable<IStatsCollector> collectors)
    {
        _collectors = collectors;
    }
    
    public async ValueTask TrackIngress(Guid id, ulong amount)
    {
        foreach (var collector in _collectors)
        {
            await collector.TrackIngress(id, amount);
        }
    }
    
    public async ValueTask TrackEgress(Guid id, ulong amount)
    {
        foreach (var collector in _collectors)
        {
            await collector.TrackEgress(id, amount);
        }
    }
}
