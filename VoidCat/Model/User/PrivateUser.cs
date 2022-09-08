namespace VoidCat.Model.User;

/// <summary>
/// A user object which includes the Email
/// </summary>
public class PrivateUser : User
{
    /// <summary>
    /// Users email address
    /// </summary>
    public string Email { get; set; } = null!;
    
    /// <summary>
    /// Users storage system for new uploads
    /// </summary>
    public string? Storage { get; set; }
}