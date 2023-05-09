namespace VoidCat.Database;

public class EmailVerification
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public User User { get; init; } = null!;

    public Guid Code { get; init; } = Guid.NewGuid();
    public DateTime Expires { get; init; }
}
