using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// Main interface for getting file info to serve to clients.
/// This interface should wrap all stores and return the combined result
/// </summary>
public interface IFileInfoManager
{
    ValueTask<PublicVoidFile?> Get(Guid id);
    ValueTask<IReadOnlyList<PublicVoidFile>> Get(Guid[] ids);
    
    /// <summary>
    /// Deletes all file metadata
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask Delete(Guid id);
}
