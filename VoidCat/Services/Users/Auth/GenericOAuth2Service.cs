using Newtonsoft.Json;
using VoidCat.Model.User;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users.Auth;

/// <summary>
/// Generic base class for OAuth2 code grant flow
/// </summary>
public abstract class GenericOAuth2Service<TDto> : IOAuthProvider
{
    private readonly HttpClient _client;

    protected GenericOAuth2Service(HttpClient client)
    {
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
        var dto = JsonConvert.DeserializeObject<TDto>(json);
        return TransformDto(dto!);
    }

    /// <inheritdoc />
    public abstract ValueTask<InternalUser?> GetUserDetails(UserAuthToken token);

    /// <summary>
    /// Build query args for authorize
    /// </summary>
    /// <returns></returns>
    protected abstract Dictionary<string, string> BuildAuthorizeQuery();

    /// <summary>
    /// Build query args for token generation
    /// </summary>
    /// <returns></returns>
    protected abstract Dictionary<string, string> BuildTokenQuery(string code);

    /// <summary>
    /// Transform DTO to <see cref="UserAuthToken"/>
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    protected abstract UserAuthToken TransformDto(TDto dto);

    /// <summary>
    /// Authorize url for this service
    /// </summary>
    protected abstract Uri AuthorizeEndpoint { get; }

    /// <summary>
    /// Generate token url for this service
    /// </summary>
    protected abstract Uri TokenEndpoint { get; }
}