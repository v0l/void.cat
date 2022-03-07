namespace VoidCat.Model;

public sealed class VirusScanResult
{
    public DateTimeOffset ScanTime { get; init; } = DateTimeOffset.UtcNow;
    
    public bool IsVirus { get; init; }

    public List<string> VirusNames { get; init; } = new();
}