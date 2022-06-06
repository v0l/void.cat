using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace VoidCat.Model;

/// <summary>
/// The base user object for the system
/// </summary>
public abstract class VoidUser
{
    protected VoidUser(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Unique Id of the user
    /// </summary>
    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid Id { get; }

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
        return new(Id)
        {
            Roles = Roles,
            Created = Created,
            LastLogin = LastLogin,
            Avatar = Avatar
        };
    }
}

/// <summary>
/// Internal user object used by the system
/// </summary>
public sealed class InternalVoidUser : PrivateVoidUser
{
    /// <inheritdoc />
    public InternalVoidUser(Guid id, string email, string passwordHash) : base(id, email)
    {
        PasswordHash = passwordHash;
    }

    /// <inheritdoc />
    public InternalVoidUser(Guid Id, string Email, string Password, DateTime Created, DateTime LastLogin,
        string Avatar,
        string DisplayName, int Flags) : base(Id, Email)
    {
        PasswordHash = Password;
    }

    /// <summary>
    /// A password hash for the user in the format <see cref="Extensions.HashPassword"/>
    /// </summary>
    public string PasswordHash { get; }
}

/// <summary>
/// A user object which includes the Email
/// </summary>
public class PrivateVoidUser : VoidUser
{
    /// <inheritdoc />
    public PrivateVoidUser(Guid id, string email) : base(id)
    {
        Email = email;
    }

    /// <summary>
    /// Full constructor for Dapper
    /// </summary>
    /// <param name="id"></param>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="created"></param>
    /// <param name="last_login"></param>
    /// <param name="avatar"></param>
    /// <param name="display_name"></param>
    /// <param name="flags"></param>
    public PrivateVoidUser(Guid Id, String Email, string Password, DateTime Created, DateTime LastLogin, string Avatar,
        string DisplayName, int Flags) : base(Id)
    {
        this.Email = Email;
        this.Created = Created;
        this.LastLogin = LastLogin;
        this.Avatar = Avatar;
        this.DisplayName = DisplayName;
        this.Flags = (VoidUserFlags) Flags;
    }

    public string Email { get; }
}

/// <inheritdoc />
public sealed class PublicVoidUser : VoidUser
{
    /// <inheritdoc />
    public PublicVoidUser(Guid id) : base(id)
    {
    }
}

[Flags]
public enum VoidUserFlags
{
    PublicProfile = 1,
    PublicUploads = 2,
    EmailVerified = 4
}