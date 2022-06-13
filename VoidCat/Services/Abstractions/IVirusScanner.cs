using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// File virus scanning interface
/// </summary>
public interface IVirusScanner
{
    /// <summary>
    /// Scan a single file
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    ValueTask<VirusScanResult> ScanFile(Guid id, CancellationToken cts);
}