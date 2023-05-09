using System.IdentityModel.Tokens.Jwt;
using VoidCat.Database;
using VoidCat.Model;

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
    public override ValueTask<User?> GetUserDetails(UserAuthToken token)
    {
        var jwt = new JwtSecurityToken(token.IdToken);

        string? GetPayloadValue(string key)
            => jwt.Payload.TryGetValue(key, out var v)
                ? v as string
                : default;

        return ValueTask.FromResult(new User()
        {
            Id = Guid.NewGuid(),
            Created = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow,
            AuthType = UserAuthType.OAuth2,
            Email = GetPayloadValue("email") ?? throw new InvalidOperationException("Failed to get email from Google JWT"),
            DisplayName = GetPayloadValue("name") ?? "void user",
            Avatar = GetPayloadValue("picture")
        })!;
    }

    /// <inheritdoc />
    protected override string Prompt => "select_account";

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