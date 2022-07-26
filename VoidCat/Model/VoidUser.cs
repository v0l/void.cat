using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace VoidCat.Model;

/// <summary>
/// The base user object for the system
/// </summary>
public abstract class VoidUser
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
    public VoidUserFlags Flags { get; set; } = VoidUserFlags.PublicProfile;

    /// <summary>
    /// Returns the Public object for this user
    /// </summary>
    /// <returns></returns>
    public PublicVoidUser ToPublic()
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

/// <summary>
/// Internal user object used by the system
/// </summary>
public sealed class InternalVoidUser : PrivateVoidUser
{
    /// <summary>
    /// A password hash for the user in the format <see cref="Extensions.HashPassword"/>
    /// </summary>
    public string Password { get; init; } = null!;
}

/// <summary>
/// A user object which includes the Email
/// </summary>
public class PrivateVoidUser : VoidUser
{
    /// <summary>
    /// Users email address
    /// </summary>
    public string Email { get; set; } = null!;
    
    /// <summary>
    /// Users storage system for new uploads
    /// </summary>
    public string? Storage { get; set; }
}

/// <inheritdoc />
public sealed class PublicVoidUser : VoidUser
{
}

[Flags]
public enum VoidUserFlags
{
    PublicProfile = 1,
    PublicUploads = 2,
    EmailVerified = 4
}