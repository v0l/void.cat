using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Stats;

/// <summary>
/// Empty time series reporter
/// </summary>
public class NoTimeSeriesStatsReporter : ITimeSeriesStatsReporter
{
    /// <inheritdoc />
    public ValueTask<IReadOnlyList<BandwidthPoint>> GetBandwidth(DateTime start, DateTime end)
    {
        return ValueTask.FromResult<IReadOnlyList<BandwidthPoint>>(new List<BandwidthPoint>());
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<BandwidthPoint>> GetBandwidth(Guid id, DateTime start, DateTime end)
    {
        return ValueTask.FromResult<IReadOnlyList<BandwidthPoint>>(new List<BandwidthPoint>());
    }
}