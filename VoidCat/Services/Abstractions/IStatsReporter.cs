using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// Get metrics from the system
/// </summary>
public interface IStatsReporter
{
    /// <summary>
    /// Get global total bandwidth
    /// </summary>
    /// <returns></returns>
    ValueTask<Bandwidth> GetBandwidth();

    /// <summary>
    /// Get global bandwidth for a single file
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask<Bandwidth> GetBandwidth(Guid id);

    /// <summary>
    /// Delete bandwidth data for a single file
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask Delete(Guid id);
}