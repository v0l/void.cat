using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users.Auth;

/// <summary>
/// Factory class to access specific OAuth providers
/// </summary>
public sealed class OAuthFactory
{
    private readonly IEnumerable<IOAuthProvider> _providers;

    public OAuthFactory(IEnumerable<IOAuthProvider> providers)
    {
        _providers = providers;
    }

    /// <summary>
    /// Get an OAuth provider by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IOAuthProvider GetProvider(string id)
    {
        var provider = _providers.FirstOrDefault(a => a.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase));
        if (provider == default)
        {
            throw new Exception($"OAuth provider not found: {id}");
        }

        return provider;
    }
}