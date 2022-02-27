using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IUserUploadsStore
{
    ValueTask<PagedResult<PublicVoidFile>> ListFiles(Guid user, PagedRequest request);
    ValueTask AddFile(Guid user, PrivateVoidFile voidFile);
}
