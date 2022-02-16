using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IFileMetadataStore
{
    ValueTask<InternalVoidFile?> Get(Guid id);
    
    ValueTask Set(InternalVoidFile meta);

    ValueTask Update(VoidFile patch, Guid editSecret);
}
