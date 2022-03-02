using Newtonsoft.Json;

namespace VoidCat.Model;

public abstract class VoidUser
{
    protected VoidUser(Guid id)
    {
        Id = id;
    }

    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid Id { get; }

    public HashSet<string> Roles { get; init; } = new() {Model.Roles.User};

    public DateTimeOffset Created { get; init; }

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

public sealed class InternalVoidUser : PrivateVoidUser
{
    public InternalVoidUser(Guid id, string email, string passwordHash) : base(id, email)
    {
        PasswordHash = passwordHash;
    }

    public string PasswordHash { get; }
}

public class PrivateVoidUser : VoidUser
{
    public PrivateVoidUser(Guid id, string email) : base(id)
    {
        Email = email;
    }

    public string Email { get; }
}

public sealed class PublicVoidUser : VoidUser
{
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
