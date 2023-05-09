using VoidCat.Database;
using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// User store
/// </summary>
public interface IUserStore
{
    /// <summary>
    /// Get a single user
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask<User?> Get(Guid id);

    /// <summary>
    /// Add a new user to the store
    /// </summary>
    /// <param name="u"></param>
    /// <returns></returns>
    ValueTask Add(User u);
    
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
    ValueTask<PagedResult<User>> ListUsers(PagedRequest request);
    
    /// <summary>
    /// Update a users profile
    /// </summary>
    /// <param name="newUser"></param>
    /// <returns></returns>
    ValueTask UpdateProfile(User newUser);

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
    ValueTask AdminUpdateUser(User user);

    /// <summary>
    /// Delete a user from the system
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask Delete(Guid id);
}