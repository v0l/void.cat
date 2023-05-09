namespace VoidCat.Database;

public class ApiKey
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public User User { get; init; } = null!;
    public string Token { get; init; } = null!;
    public DateTime Expiry { get; init; }
    public DateTime Created { get; init; }
}
