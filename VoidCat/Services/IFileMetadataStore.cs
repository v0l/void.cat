using VoidCat.Model;

namespace VoidCat.Services;

public interface IFileMetadataStore
{
    Task<InternalVoidFile?> Get(Guid id);
    
    Task Set(InternalVoidFile meta);

    Task Update(VoidFile patch, Guid editSecret);
}
