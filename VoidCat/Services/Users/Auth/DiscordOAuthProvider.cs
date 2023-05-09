using Newtonsoft.Json;
using VoidCat.Database;
using VoidCat.Model;

namespace VoidCat.Services.Users.Auth;

/// <inheritdoc />
public class DiscordOAuthProvider : GenericOAuth2Service
{
    private readonly HttpClient _client;

    public DiscordOAuthProvider(HttpClient client, VoidSettings settings) : base(client, settings)
    {
        _client = client;
        Details = settings.Discord!;
    }

    /// <inheritdoc />
    public override string Id => "discord";

    /// <inheritdoc />
    public override async ValueTask<User?> GetUserDetails(UserAuthToken token)
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
                AuthType = UserAuthType.OAuth2,
                DisplayName = $"{user!.Username}",
                Avatar = !string.IsNullOrEmpty(user.Avatar)
                    ? $"https://cdn.discordapp.com/avatars/{user.Id}/{user.Avatar}.png"
                    : null,
                Email = user.Email!,
                Created = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow
            };
        }

        return default;
    }

    /// <inheritdoc />
    protected override Uri AuthorizeEndpoint => new("https://discord.com/oauth2/authorize");

    /// <inheritdoc />
    protected override Uri TokenEndpoint => new("https://discord.com/api/oauth2/token");

    /// <inheritdoc />
    protected override OAuthDetails Details { get; }

    /// <inheritdoc />
    protected override string[] Scopes => new[] {"email", "identify"};

    internal class DiscordUser
    {
        [JsonProperty("id")] public string Id { get; init; } = null!;

        [JsonProperty("username")] public string Username { get; init; } = null!;

        [JsonProperty("discriminator")] public string Discriminator { get; init; } = null!;

        [JsonProperty("avatar")] public string? Avatar { get; init; }

        [JsonProperty("email")] public string? Email { get; init; }
    }
}