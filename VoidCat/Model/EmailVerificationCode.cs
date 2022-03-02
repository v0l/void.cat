namespace VoidCat.Model;

public class EmailVerificationCode
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public DateTimeOffset Expires { get; init; }
}