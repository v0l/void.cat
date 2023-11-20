namespace VoidCat.Database;

public record File
{
    public Guid Id { get; init; }
    public string? Name { get; set; }
    public ulong Size { get; init; }
    public DateTime Uploaded { get; init; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public string MimeType { get; set; } = "application/octet-stream";
    public string? Digest { get; init; }
    public Guid EditSecret { get; init; }
    public DateTime? Expires { get; set; }
    public string Storage { get; set; } = "local-disk";
    public string? EncryptionParams { get; set; }
    public string? MagnetLink { get; set; }
    public string? OriginalDigest { get; init; }
    public string? MediaDimensions { get; init; }
    
    public Paywall? Paywall { get; init; }
}
