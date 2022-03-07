namespace VoidCat.Services.Abstractions;

public interface IPublicPrivateStore<TPublic, in TPrivate>
{
    ValueTask<TPublic?> Get(Guid id);
    
    ValueTask Set(Guid id, TPrivate obj);

    ValueTask Delete(Guid id);
}