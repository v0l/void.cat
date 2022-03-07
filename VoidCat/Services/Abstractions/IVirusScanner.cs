using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IVirusScanner
{
    ValueTask<VirusScanResult> ScanFile(Guid id, CancellationToken cts);
}