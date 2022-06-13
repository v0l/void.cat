using Newtonsoft.Json;

namespace VoidCat.Model;

/// <summary>
/// Results for virus scan of a single file
/// </summary>
public sealed class VirusScanResult
{
    /// <summary>
    /// Unique Id for this scan
    /// </summary>
    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid Id { get; init; }

    /// <summary>
    /// Id of the file that was scanned
    /// </summary>
    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid File { get; init; }

    /// <summary>
    /// Time the file was scanned
    /// </summary>
    public DateTimeOffset ScanTime { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The name of the virus scanner software
    /// </summary>
    public string Scanner { get; init; } = null!;

    /// <summary>
    /// Virus detection score, this can mean different things for each scanner but the value should be between 0 and 1
    /// </summary>
    public decimal Score { get; init; }

    /// <summary>
    /// Detected virus names
    /// </summary>
    public string? Names { get; init; }

    /// <summary>
    /// If we consider this result as a virus or not
    /// </summary>
    public bool IsVirus => Score >= 0.75m && !string.IsNullOrEmpty(Names);
}