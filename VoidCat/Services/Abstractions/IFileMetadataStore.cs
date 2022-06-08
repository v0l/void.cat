using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// File metadata contains all data about a file except for the file data itself
/// </summary>
public interface IFileMetadataStore : IPublicPrivateStore<VoidFileMeta, SecretVoidFileMeta>
{
    /// <summary>
    /// Get metadata for a single file
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="TMeta"></typeparam>
    /// <returns></returns>
    ValueTask<TMeta?> Get<TMeta>(Guid id) where TMeta : VoidFileMeta;
    
    /// <summary>
    /// Get metadata for multiple files
    /// </summary>
    /// <param name="ids"></param>
    /// <typeparam name="TMeta"></typeparam>
    /// <returns></returns>
    ValueTask<IReadOnlyList<TMeta>> Get<TMeta>(Guid[] ids) where TMeta : VoidFileMeta;
    
    /// <summary>
    /// Update file metadata
    /// </summary>
    /// <param name="id"></param>
    /// <param name="meta"></param>
    /// <typeparam name="TMeta"></typeparam>
    /// <returns></returns>
    ValueTask Update<TMeta>(Guid id, TMeta meta) where TMeta : VoidFileMeta;
    
    /// <summary>
    /// List all files in the store
    /// </summary>
    /// <param name="request"></param>
    /// <typeparam name="TMeta"></typeparam>
    /// <returns></returns>
    ValueTask<PagedResult<TMeta>> ListFiles<TMeta>(PagedRequest request) where TMeta : VoidFileMeta;

    /// <summary>
    /// Returns basic stats about the file store
    /// </summary>
    /// <returns></returns>
    ValueTask<StoreStats> Stats();

    /// <summary>
    /// Simple stats of the current store
    /// </summary>
    /// <param name="Files"></param>
    /// <param name="Size"></param>
    public sealed record StoreStats(long Files, ulong Size);
}