namespace VoidCat.Database;

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

public sealed class User
{
    /// <summary>
    /// Unique Id of the user
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Users email address
    /// </summary>
    public string Email { get; set; } = null!;
    
    /// <summary>
    /// Users password (hashed)
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// When the user account was created
    /// </summary>
    public DateTime Created { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The last time the user logged in
    /// </summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>
    /// Display avatar for user profile
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Display name for user profile
    /// </summary>
    public string DisplayName { get; set; } = "void user";
    
    /// <summary>
    /// Profile flags
    /// </summary>
    public UserFlags Flags { get; set; } = UserFlags.PublicProfile;
    
    /// <summary>
    /// Users storage system for new uploads
    /// </summary>
    public string Storage { get; set; } = "local-disk";
    
    /// <summary>
    /// Account authentication type
    /// </summary>
    public UserAuthType AuthType { get; init; }

    /// <summary>
    /// Roles assigned to this user which grant them extra permissions
    /// </summary>
    public List<UserRole> Roles { get; init; } = new();

    /// <summary>
    /// All files uploaded by this user
    /// </summary>
    public List<UserFile> UserFiles { get; init; } = new();
}

public class UserRole
{
    public Guid UserId { get; init; }
    public User User { get; init; }

    public string Role { get; init; } = null!;
}

public enum UserAuthType
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
