using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// Mapping store to associate files to users
/// </summary>
public interface IUserUploadsStore
{
    /// <summary>
    /// List all files for the user
    /// </summary>
    /// <param name="user"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    ValueTask<PagedResult<Guid>> ListFiles(Guid user, PagedRequest request);
    
    /// <summary>
    /// Assign a file upload to a user
    /// </summary>
    /// <param name="user"></param>
    /// <param name="voidFile"></param>
    /// <returns></returns>
    ValueTask AddFile(Guid user, PrivateVoidFile voidFile);
    
    /// <summary>
    /// Get the uploader of a single file
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    ValueTask<Guid?> Uploader(Guid file);
}
