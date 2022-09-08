using Newtonsoft.Json;

namespace VoidCat.Model.User;

/// <summary>
/// The base user object for the system
/// </summary>
public abstract class User
{
    /// <summary>
    /// Unique Id of the user
    /// </summary>
    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid Id { get; init; }

    /// <summary>
    /// Roles assigned to this user which grant them extra permissions
    /// </summary>
    public HashSet<string> Roles { get; init; } = new() {Model.Roles.User};

    /// <summary>
    /// When the user account was created
    /// </summary>
    public DateTimeOffset Created { get; init; }

    /// <summary>
    /// The last time the user logged in
    /// </summary>
    public DateTimeOffset LastLogin { get; set; }

    /// <summary>
    /// Display avatar for user profile
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Display name for user profile
    /// </summary>
    public string? DisplayName { get; set; } = "void user";

    /// <summary>
    /// Profile flags
    /// </summary>
    public UserFlags Flags { get; set; } = UserFlags.PublicProfile;

    /// <summary>
    /// Account authentication type
    /// </summary>
    public AuthType AuthType { get; init; }
    
    /// <summary>
    /// Returns the Public object for this user
    /// </summary>
    /// <returns></returns>
    public PublicUser ToPublic()
    {
        return new()
        {
            Id = Id,
            Roles = Roles,
            Created = Created,
            LastLogin = LastLogin,
            Avatar = Avatar,
            Flags = Flags
        };
    }
}