using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IFileMetadataStore
{
    ValueTask<SecretVoidFileMeta?> Get(Guid id);
    
    ValueTask Set(Guid id, SecretVoidFileMeta meta);

    ValueTask Update(Guid id, SecretVoidFileMeta patch);
}
