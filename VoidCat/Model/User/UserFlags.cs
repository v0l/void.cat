namespace VoidCat.Model.User;

/// <summary>
/// Account status flags
/// </summary>
[Flags]
public enum UserFlags
{
    /// <summary>
    /// Profile is public
    /// </summary>
    PublicProfile = 1,
    
    /// <summary>
    /// Uploads list is public
    /// </summary>
    PublicUploads = 2,
    
    /// <summary>
    /// Account has email verified
    /// </summary>
    EmailVerified = 4
}