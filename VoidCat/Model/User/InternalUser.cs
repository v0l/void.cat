namespace VoidCat.Model.User;

/// <summary>
/// Internal user object used by the system
/// </summary>
public sealed class InternalUser : PrivateUser
{
    /// <summary>
    /// A password hash for the user in the format <see cref="Extensions.HashPassword"/>
    /// </summary>
    public string Password { get; init; } = null!;
}