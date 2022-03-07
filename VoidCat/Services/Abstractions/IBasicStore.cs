namespace VoidCat.Services.Abstractions;

public interface IBasicStore<T>
{
    ValueTask<T?> Get(Guid id);
    
    ValueTask Set(Guid id, T obj);

    ValueTask Delete(Guid id);

    string MapKey(Guid id);
}