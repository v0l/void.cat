using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IFileInfoManager
{
    ValueTask<PublicVoidFile?> Get(Guid id);
}
