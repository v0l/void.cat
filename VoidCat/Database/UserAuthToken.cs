namespace VoidCat.Database;

public class UserAuthToken
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public User User { get; init; } = null!;
    public string Provider { get; init; } = null!;
    public string AccessToken { get; init; } = null!;
    public string TokenType { get; init; } = null!;
    public DateTime Expires { get; init; }
    public string RefreshToken { get; init; } = null!;
    public string Scope { get; init; } = null!;
    public string? IdToken { get; init; }
}
