namespace VoidCat.Services.Abstractions;

public interface ICache
{
    ValueTask<T?> Get<T>(string key);
    ValueTask Set<T>(string key, T value, TimeSpan? expire = null);
    ValueTask Delete(string key);
    
    ValueTask<string[]> GetList(string key);
    ValueTask AddToList(string key, string value);
    ValueTask RemoveFromList(string key, string value);

}
