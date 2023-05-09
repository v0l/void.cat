using VoidCat.Database;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// Store for virus scan results
/// </summary>
public interface IVirusScanStore : IBasicStore<VirusScanResult>
{
    /// <summary>
    /// Get the latest scan result by file id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask<VirusScanResult?> GetByFile(Guid id);
}