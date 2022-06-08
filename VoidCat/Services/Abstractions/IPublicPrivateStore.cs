namespace VoidCat.Services.Abstractions;

/// <summary>
/// Store interface where there is a public and private model
/// </summary>
/// <typeparam name="TPublic"></typeparam>
/// <typeparam name="TPrivate"></typeparam>
public interface IPublicPrivateStore<TPublic, TPrivate>
{
    /// <summary>
    /// Get the public model
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask<TPublic?> Get(Guid id);

    /// <summary>
    /// Get the private model (contains sensitive data)
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask<TPrivate?> GetPrivate(Guid id);

    /// <summary>
    /// Set the private obj in the store
    /// </summary>
    /// <param name="id"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    ValueTask Set(Guid id, TPrivate obj);

    /// <summary>
    /// Delete the object from the store
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask Delete(Guid id);
}