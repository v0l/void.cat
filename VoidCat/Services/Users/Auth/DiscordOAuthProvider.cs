using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Model.User;

namespace VoidCat.Services.Users.Auth;

/// <inheritdoc />
public class DiscordOAuthProvider : GenericOAuth2Service<DiscordAccessToken>
{
    private readonly HttpClient _client;
    private readonly DiscordSettings _discord;
    private readonly Uri _site;

    public DiscordOAuthProvider(HttpClient client, VoidSettings settings) : base(client)
    {
        _client = client;
        _discord = settings.Discord!;
        _site = settings.SiteUrl;
    }

    /// <inheritdoc />
    public override string Id => "discord";

    /// <inheritdoc />
    public override async ValueTask<InternalUser?> GetUserDetails(UserAuthToken token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
        req.Headers.Authorization = new("Bearer", token.AccessToken);

        var rsp = await _client.SendAsync(req);
        if (rsp.IsSuccessStatusCode)
        {
            var user = JsonConvert.DeserializeObject<DiscordUser>(await rsp.Content.ReadAsStringAsync());
            return new()
            {
                Id = Guid.NewGuid(),
                AuthType = AuthType.OAuth2,
                DisplayName = $"{user!.Username}",
                Avatar = !string.IsNullOrEmpty(user.Avatar)
                    ? $"https://cdn.discordapp.com/avatars/{user.Id}/{user.Avatar}.png"
                    : null,
                Email = user.Email!,
                Created = DateTimeOffset.UtcNow,
                LastLogin = DateTimeOffset.UtcNow
            };
        }

        return default;
    }

    /// <inheritdoc />
    protected override Dictionary<string, string> BuildAuthorizeQuery()
        => new()
        {
            {"response_type", "code"},
            {"client_id", _discord.ClientId!},
            {"scope", "email identify"},
            {"prompt", "none"},
            {"redirect_uri", new Uri(_site, $"/auth/{Id}/token").ToString()}
        };

    protected override Dictionary<string, string> BuildTokenQuery(string code)
        => new()
        {
            {"client_id", _discord.ClientId!},
            {"client_secret", _discord.ClientSecret!},
            {"grant_type", "authorization_code"},
            {"code", code},
            {"redirect_uri", new Uri(_site, $"/auth/{Id}/token").ToString()}
        };

    /// <inheritdoc />
    protected override UserAuthToken TransformDto(DiscordAccessToken dto)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Provider = Id,
            AccessToken = dto.AccessToken,
            Expires = DateTime.UtcNow.AddSeconds(dto.ExpiresIn),
            TokenType = dto.TokenType,
            RefreshToken = dto.RefreshToken,
            Scope = dto.Scope
        };
    }

    /// <inheritdoc />
    protected override Uri AuthorizeEndpoint => new("https://discord.com/oauth2/authorize");

    /// <inheritdoc />
    protected override Uri TokenEndpoint => new("https://discord.com/api/oauth2/token");
}

public class DiscordAccessToken
{
    [JsonProperty("access_token")] public string AccessToken { get; init; }

    [JsonProperty("expires_in")] public int ExpiresIn { get; init; }

    [JsonProperty("token_type")] public string TokenType { get; init; }

    [JsonProperty("refresh_token")] public string RefreshToken { get; init; }

    [JsonProperty("scope")] public string Scope { get; init; }
}

internal class DiscordUser
{
    [JsonProperty("id")] public string Id { get; init; } = null!;

    [JsonProperty("username")] public string Username { get; init; } = null!;

    [JsonProperty("discriminator")] public string Discriminator { get; init; } = null!;

    [JsonProperty("avatar")] public string? Avatar { get; init; }

    [JsonProperty("email")] public string? Email { get; init; }
}