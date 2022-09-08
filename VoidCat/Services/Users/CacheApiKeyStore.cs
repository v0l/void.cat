using VoidCat.Model.User;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

/// <inheritdoc cref="VoidCat.Services.Abstractions.IApiKeyStore" />
public class CacheApiKeyStore : BasicCacheStore<ApiKey>, IApiKeyStore
{
    public CacheApiKeyStore(ICache cache) : base(cache)
    {
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<ApiKey>> ListKeys(Guid id)
    {
        throw new NotImplementedException();
    }

    protected override string MapKey(Guid id) => $"api-key:{id}";
}
