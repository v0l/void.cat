using Newtonsoft.Json;
using VoidCat.Services.Abstractions;

// ReSharper disable InconsistentNaming

namespace VoidCat.Model;

/// <summary>
/// Base metadata must contain version number
/// </summary>
public interface IVoidFileMeta
{
    const int CurrentVersion = 3;

    int Version { get; init; }
}

/// <summary>
/// File metadata which is managed by <see cref="IFileMetadataStore"/>
/// </summary>
public record VoidFileMeta : IVoidFileMeta
{
    /// <summary>
    /// Metadata version
    /// </summary>
    public int Version { get; init; } = IVoidFileMeta.CurrentVersion;

    /// <summary>
    /// Filename
    /// </summary>
    public string? Name { get; set; }

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
    public string? Description { get; set; }

    /// <summary>
    /// The content type of the file
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// SHA-256 hash of the file
    /// </summary>
    public string? Digest { get; init; }

    /// <summary>
    /// Url to download the file
    /// </summary>
    public Uri? Url { get; set; }

    /// <summary>
    /// User who uploaded the file
    /// </summary>
    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid? Uploader { get; init; }
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