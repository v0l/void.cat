namespace VoidCat.Model;

/// <summary>
/// Results for virus scan of a single file
/// </summary>
public sealed class VirusStatus
{
    /// <summary>
    /// Time the file was scanned
    /// </summary>
    public DateTime ScanTime { get; init; }

    /// <summary>
    /// Detected virus names
    /// </summary>
    public string? Names { get; init; }

    /// <summary>
    /// If we consider this result as a virus or not
    /// </summary>
    public bool IsVirus { get; init; }
}