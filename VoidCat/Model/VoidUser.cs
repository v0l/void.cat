using Newtonsoft.Json;
using VoidCat.Model;

namespace VoidCat.Model;

public abstract class VoidUser
{
    protected VoidUser(Guid id, string email)
    {
        Id = id;
        Email = email;
    }

    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid Id { get; }

    public string Email { get; }

    public HashSet<string> Roles { get; init; } = new() { Model.Roles.User };

    public DateTimeOffset Created { get; init; }

    public DateTimeOffset LastLogin { get; set; }
    
    public string? Avatar { get; set; }

    public PublicVoidUser ToPublic()
    {
        return new(Id, Email)
        {
            Roles = Roles,
            Created = Created,
            LastLogin = LastLogin,
            Avatar = Avatar
        };
    }
}

public sealed class PrivateVoidUser : VoidUser
{
    public PrivateVoidUser(Guid id, string email, string passwordHash) : base(id, email)
    {
        PasswordHash = passwordHash;
    }

    public string PasswordHash { get; }
}

public sealed class PublicVoidUser : VoidUser
{
    public PublicVoidUser(Guid id, string email) : base(id, email)
    {
    }
}