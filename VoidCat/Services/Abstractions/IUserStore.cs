using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IUserStore
{
    ValueTask<Guid?> LookupUser(string email);
    ValueTask<T?> Get<T>(Guid id) where T : VoidUser;
    ValueTask Set(PrivateVoidUser user);
    ValueTask<PagedResult<PublicVoidUser>> ListUsers(PagedRequest request);
}