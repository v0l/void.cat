using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface ITimeSeriesStatsReporter
{
    ValueTask<IReadOnlyList<BandwidthPoint>> GetBandwidth(DateTime start, DateTime end);
    ValueTask<IReadOnlyList<BandwidthPoint>> GetBandwidth(Guid id, DateTime start, DateTime end);
}