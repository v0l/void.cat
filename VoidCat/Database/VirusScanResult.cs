namespace VoidCat.Database;

public class VirusScanResult
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FileId { get; init; }
    public File File { get; init; } = null!;
    public DateTime ScanTime { get; init; }
    public string Scanner { get; init; } = null!;
    public decimal Score { get; init; }
    public string Names { get; init; } = null!;
}
