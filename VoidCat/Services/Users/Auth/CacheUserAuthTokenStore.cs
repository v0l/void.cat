using VoidCat.Model.User;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users.Auth;

/// <inheritdoc cref="IUserAuthTokenStore"/>
public class CacheUserAuthTokenStore : BasicCacheStore<UserAuthToken>, IUserAuthTokenStore
{
    public CacheUserAuthTokenStore(ICache cache) : base(cache)
    {
    }

    /// <inheritdoc />
    protected override string MapKey(Guid id) => $"auth-token:{id}";
}