using Newtonsoft.Json;
using VoidCat.Database;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users.Auth;

/// <summary>
/// Generic base class for OAuth2 code grant flow
/// </summary>
public abstract class GenericOAuth2Service : IOAuthProvider
{
    private readonly Uri _uri;
    private readonly HttpClient _client;

    protected GenericOAuth2Service(HttpClient client, VoidSettings settings)
    {
        _uri = settings.SiteUrl;
        _client = client;
    }

    /// <inheritdoc />
    public abstract string Id { get; }

    /// <inheritdoc />
    public Uri Authorize()
    {
        var ub = new UriBuilder(AuthorizeEndpoint)
        {
            Query = string.Join("&", BuildAuthorizeQuery().Select(a => $"{a.Key}={Uri.EscapeDataString(a.Value)}"))
        };

        return ub.Uri;
    }

    /// <inheritdoc />
    public async ValueTask<UserAuthToken> GetToken(string code)
    {
        var form = new FormUrlEncodedContent(BuildTokenQuery(code));
        var rsp = await _client.PostAsync(TokenEndpoint, form);
        var json = await rsp.Content.ReadAsStringAsync();
        if (!rsp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to get token from provider: {Id}, response: {json}");
        }

        var dto = JsonConvert.DeserializeObject<OAuthAccessToken>(json);
        return TransformDto(dto!);
    }

    /// <inheritdoc />
    public abstract ValueTask<User?> GetUserDetails(UserAuthToken token);

    /// <summary>
    /// Build query args for authorize
    /// </summary>
    /// <returns></returns>
    protected virtual Dictionary<string, string> BuildAuthorizeQuery()
        => new()
        {
            {"response_type", "code"},
            {"client_id", Details.ClientId!},
            {"scope", string.Join(" ", Scopes)},
            {"prompt", Prompt},
            {"redirect_uri", new Uri(_uri, $"/auth/{Id}/token").ToString()}
        };

    /// <summary>
    /// Build query args for token generation
    /// </summary>
    /// <returns></returns>
    protected virtual Dictionary<string, string> BuildTokenQuery(string code)
        => new()
        {
            {"client_id", Details.ClientId!},
            {"client_secret", Details.ClientSecret!},
            {"grant_type", "authorization_code"},
            {"code", code},
            {"redirect_uri", new Uri(_uri, $"/auth/{Id}/token").ToString()}
        };

    /// <summary>
    /// Prompt type for authorization
    /// </summary>
    protected virtual string Prompt => "none";

    /// <summary>
    /// Authorize url for this service
    /// </summary>
    protected abstract Uri AuthorizeEndpoint { get; }

    /// <summary>
    /// Generate token url for this service
    /// </summary>
    protected abstract Uri TokenEndpoint { get; }

    /// <summary>
    /// OAuth client details
    /// </summary>
    protected abstract OAuthDetails Details { get; }

    /// <summary>
    /// OAuth scopes
    /// </summary>
    protected abstract string[] Scopes { get; }

    /// <summary>
    /// Transform DTO to <see cref="UserAuthToken"/>
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    protected virtual UserAuthToken TransformDto(OAuthAccessToken dto)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Provider = Id,
            AccessToken = dto.AccessToken,
            Expires = DateTime.UtcNow.AddSeconds(dto.ExpiresIn),
            TokenType = dto.TokenType,
            RefreshToken = dto.RefreshToken,
            Scope = dto.Scope,
            IdToken = dto.IdToken
        };
    }

    protected class OAuthAccessToken
    {
        [JsonProperty("access_token")] 
        public string AccessToken { get; init; }

        [JsonProperty("expires_in")] 
        public int ExpiresIn { get; init; }

        [JsonProperty("token_type")] 
        public string TokenType { get; init; }

        [JsonProperty("refresh_token")] 
        public string RefreshToken { get; init; }

        [JsonProperty("scope")] 
        public string Scope { get; init; }
        
        [JsonProperty("id_token")]
        public string IdToken { get; init; }
    }
}