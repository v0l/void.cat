using VoidCat.Model;
using File = VoidCat.Database.File;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// File metadata contains all data about a file except for the file data itself
/// </summary>
public interface IFileMetadataStore
{
    /// <summary>
    /// Get metadata for a single file
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask<File?> Get(Guid id);
    
    /// <summary>
    /// Get metadata for multiple files
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    ValueTask<IReadOnlyList<File>> Get(Guid[] ids);

    /// <summary>
    /// Add new file metadata to this store
    /// </summary>
    /// <param name="f"></param>
    /// <returns></returns>
    ValueTask Add(File f);
    
    /// <summary>
    /// Update file metadata
    /// </summary>
    /// <param name="id"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    ValueTask Update(Guid id, File meta);
    
    /// <summary>
    /// List all files in the store
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    ValueTask<PagedResult<File>> ListFiles(PagedRequest request);

    /// <summary>
    /// Returns basic stats about the file store
    /// </summary>
    /// <returns></returns>
    ValueTask<StoreStats> Stats();

    /// <summary>
    /// Delete metadata object from the store
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask Delete(Guid id);

    /// <summary>
    /// Simple stats of the current store
    /// </summary>
    /// <param name="Files"></param>
    /// <param name="Size"></param>
    public sealed record StoreStats(long Files, ulong Size);
}