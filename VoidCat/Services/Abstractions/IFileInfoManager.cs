using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// Main interface for getting file info to serve to clients.
/// This interface should wrap all stores and return the combined result
/// </summary>
public interface IFileInfoManager
{
    /// <summary>
    /// Get all metadata for a single file
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask<PublicVoidFile?> Get(Guid id);
    
    /// <summary>
    /// Get all metadata for multiple files
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    ValueTask<IReadOnlyList<PublicVoidFile>> Get(Guid[] ids);
    
    /// <summary>
    /// Deletes all file metadata
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask Delete(Guid id);
}
