using System.IdentityModel.Tokens.Jwt;
using VoidCat.Model;
using VoidCat.Model.User;

namespace VoidCat.Services.Users.Auth;

public class GoogleOAuthProvider : GenericOAuth2Service
{
    private readonly HttpClient _client;

    public GoogleOAuthProvider(HttpClient client, VoidSettings settings) : base(client, settings)
    {
        _client = client;
        Details = settings.Google!;
    }

    /// <inheritdoc />
    public override string Id => "google";

    /// <inheritdoc />
    public override ValueTask<InternalUser?> GetUserDetails(UserAuthToken token)
    {
        var jwt = JwtPayload.Base64UrlDeserialize(token.AccessToken);
        return ValueTask.FromResult(new InternalUser()
        {
            Id = Guid.NewGuid(),
            Created = DateTimeOffset.UtcNow,
            LastLogin = DateTimeOffset.UtcNow,
            AuthType = AuthType.OAuth2,
            Email = jwt.Jti,
            DisplayName = jwt.Acr
        })!;
    }

    /// <inheritdoc />
    protected override Uri AuthorizeEndpoint => new("https://accounts.google.com/o/oauth2/v2/auth");

    /// <inheritdoc />
    protected override Uri TokenEndpoint => new("https://oauth2.googleapis.com/token");

    /// <inheritdoc />
    protected override OAuthDetails Details { get; }

    /// <inheritdoc />
    protected override string[] Scopes => new[]
        {"https://www.googleapis.com/auth/userinfo.email", "https://www.googleapis.com/auth/userinfo.profile"};
}

public sealed class GoogleUserAccount
{
}