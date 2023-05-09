namespace VoidCat.Model;

public class AdminApiUser : ApiUser
{
    public string Storage { get; init; } = null!;
    public string Email { get; init; } = null!;
}
