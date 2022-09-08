namespace VoidCat.Model.User;

/// <summary>
/// User account authentication type
/// </summary>
public enum AuthType
{
    /// <summary>
    /// Encrypted password 
    /// </summary>
    Internal = 0,
    
    /// <summary>
    /// PGP challenge
    /// </summary>
    PGP = 1,
    
    /// <summary>
    /// OAuth2 token
    /// </summary>
    OAuth2 = 2,
    
    /// <summary>
    /// Lightning node challenge
    /// </summary>
    Lightning = 3
}