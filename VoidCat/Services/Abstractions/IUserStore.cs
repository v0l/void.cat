using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// User store
/// </summary>
public interface IUserStore : IPublicPrivateStore<VoidUser, InternalVoidUser>
{
    /// <summary>
    /// Get a single user
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    ValueTask<T?> Get<T>(Guid id) where T : VoidUser;
    
    /// <summary>
    /// Lookup a user by their email address
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    ValueTask<Guid?> LookupUser(string email);
    
    /// <summary>
    /// List all users in the system
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    ValueTask<PagedResult<PrivateVoidUser>> ListUsers(PagedRequest request);
    
    /// <summary>
    /// Update a users profile
    /// </summary>
    /// <param name="newUser"></param>
    /// <returns></returns>
    ValueTask UpdateProfile(PublicVoidUser newUser);

    /// <summary>
    /// Updates the last login timestamp for the user
    /// </summary>
    /// <param name="id"></param>
    /// <param name="timestamp"></param>
    /// <returns></returns>
    ValueTask UpdateLastLogin(Guid id, DateTime timestamp);

    /// <summary>
    /// Update user account for admin
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    ValueTask AdminUpdateUser(PrivateVoidUser user);
}