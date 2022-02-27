using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IFileMetadataStore
{
    ValueTask<TMeta?> Get<TMeta>(Guid id) where TMeta : VoidFileMeta;
    
    ValueTask Set(Guid id, SecretVoidFileMeta meta);

    ValueTask Update(Guid id, SecretVoidFileMeta patch);

    ValueTask Delete(Guid id);
}
