namespace VoidCat.Services.Abstractions;

/// <summary>
/// Basic KV cache interface
/// </summary>
public interface ICache
{
    /// <summary>
    /// Get a single object from cache by its key
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    ValueTask<T?> Get<T>(string key);
    
    /// <summary>
    /// Set the the value of a key in the cache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expire"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    ValueTask Set<T>(string key, T value, TimeSpan? expire = null);
    
    /// <summary>
    /// Delete an object from the cache
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    ValueTask Delete(string key);
    
    /// <summary>
    /// Return a list of items at the specified key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    ValueTask<string[]> GetList(string key);
    
    /// <summary>
    /// Add an item to the list at the specified key
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    ValueTask AddToList(string key, string value);
    
    /// <summary>
    /// Remove an item from the list at a the specified key
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    ValueTask RemoveFromList(string key, string value);

}
