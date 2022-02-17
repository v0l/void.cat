using Newtonsoft.Json;
using VoidCat.Services.Abstractions;

namespace VoidCat.Model;

/// <summary>
/// File metadata which is managed by <see cref="IFileMetadataStore"/>
/// </summary>
public record VoidFileMeta
{
    /// <summary>
    /// Metadata version
    /// </summary>
    public int Version { get; init; } = 2;
    
    /// <summary>
    /// Filename
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Size of the file in storage
    /// </summary>
    public ulong Size { get; init; }

    /// <summary>
    /// Date file was uploaded
    /// </summary>
    public DateTimeOffset Uploaded { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Description about the file
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The content type of the file
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// SHA-256 hash of the file
    /// </summary>
    public string? Digest { get; init; }
}

/// <summary>
/// <see cref="VoidFile"/> with attached <see cref="EditSecret"/>
/// </summary>
public record SecretVoidFileMeta : VoidFileMeta
{
    /// <summary>
    /// A secret key used to make edits to the file after its uploaded
    /// </summary>
    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid EditSecret { get; init; }
}