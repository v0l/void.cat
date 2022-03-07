using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IUserStore : IPublicPrivateStore<VoidUser, InternalVoidUser>
{
    ValueTask<T?> Get<T>(Guid id) where T : VoidUser;
    ValueTask Delete(PrivateVoidUser user);
    
    ValueTask<Guid?> LookupUser(string email);
    ValueTask<PagedResult<PrivateVoidUser>> ListUsers(PagedRequest request);
    ValueTask UpdateProfile(PublicVoidUser newUser);
}