namespace VoidCat.Model.User;

/// <summary>
/// OAuth2 access token
/// </summary>
public sealed class UserAuthToken
{
    public Guid Id { get; init; }
    
    public Guid User { get; init; }
    
    public string Provider { get; init; }

    public string AccessToken { get; init; }

    public string TokenType { get; init; }

    public DateTime Expires { get; init; }

    public string RefreshToken { get; init; }

    public string Scope { get; init; }
    
    public string IdToken { get; init; }
}