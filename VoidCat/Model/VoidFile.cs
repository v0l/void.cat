using Newtonsoft.Json;
using VoidCat.Database;

namespace VoidCat.Model;

/// <summary>
/// Primary response type for file information
/// </summary>
public class VoidFileResponse
{
    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid Id { get; init; }
    public VoidFileMeta Metadata { get; init; } = null!;
    public Paywall? Payment { get; init; }
    public ApiUser? Uploader { get; init; }
    public Bandwidth? Bandwidth { get; init; }
    public VirusStatus? VirusScan { get; init; }
}

public class VoidFileMeta
{
    public string? Name { get; init; }
    public ulong Size { get; init; }
    public DateTime Uploaded { get; init; }
    public string? Description { get; init; }
    public string MimeType { get; init; }
    public string? Digest { get; init; }
    
    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid? EditSecret { get; init; }
    public DateTime? Expires { get; init; }
    public string Storage { get; init; } = "local-disk";
    public string? EncryptionParams { get; init; }
    public string? MagnetLink { get; init; }
}