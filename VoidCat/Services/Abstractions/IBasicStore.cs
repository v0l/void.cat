namespace VoidCat.Services.Abstractions;

/// <summary>
/// Simple CRUD interface for data stores
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IBasicStore<T>
{
    /// <summary>
    /// Get a single item from the store
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask<T?> Get(Guid id);

    /// <summary>
    /// Add an item to the store
    /// </summary>
    /// <param name="id"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    ValueTask Add(Guid id, T obj);

    /// <summary>
    /// Delete an item from the store
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask Delete(Guid id);
}