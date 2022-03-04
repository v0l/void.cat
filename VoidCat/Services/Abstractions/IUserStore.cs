using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IUserStore
{
    ValueTask<Guid?> LookupUser(string email);
    ValueTask<T?> Get<T>(Guid id) where T : VoidUser;
    ValueTask Set(InternalVoidUser user);
    ValueTask<PagedResult<PrivateVoidUser>> ListUsers(PagedRequest request);
    ValueTask UpdateProfile(PublicVoidUser newUser);
    ValueTask Delete(PrivateVoidUser user);
}