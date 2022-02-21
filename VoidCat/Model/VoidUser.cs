namespace VoidCat.Model;

public sealed record VoidUser(Guid Id, string Email, string PasswordHash)
{
    public IEnumerable<string> Roles { get; init; } = Enumerable.Empty<string>();
}
