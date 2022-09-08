using VoidCat.Model.User;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// Api key store
/// </summary>
public interface IApiKeyStore : IBasicStore<ApiKey>
{
    /// <summary>
    /// Return a list of Api keys for a given user
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask<IReadOnlyList<ApiKey>> ListKeys(Guid id);
}
