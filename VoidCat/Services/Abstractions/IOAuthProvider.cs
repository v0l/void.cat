using VoidCat.Database;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// OAuth2 code grant provider
/// </summary>
public interface IOAuthProvider
{
    /// <summary>
    /// Id of this provider
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Generate authorization code grant uri
    /// </summary>
    /// <returns></returns>
    Uri Authorize();

    /// <summary>
    /// Get access token from auth code
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    ValueTask<UserAuthToken> GetToken(string code);

    /// <summary>
    /// Get a user object which represents this external account authorization
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    ValueTask<User?> GetUserDetails(UserAuthToken token);
}