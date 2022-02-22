using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

public class UserManager : IUserManager
{
    private readonly IUserStore _store;

    public UserManager(IUserStore store)
    {
        _store = store;
    }
    
    public async ValueTask<VoidUser> Login(string email, string password)
    {
        var userId = await _store.LookupUser(email);
        if (!userId.HasValue) throw new InvalidOperationException("User does not exist");

        var user = await _store.Get(userId.Value);
        if (!(user?.CheckPassword(password) ?? false)) throw new InvalidOperationException("User does not exist");

        return user;
    }
    
    public async ValueTask<VoidUser> Register(string email, string password)
    {
        var existingUser = await _store.LookupUser(email);
        if (existingUser != Guid.Empty) throw new InvalidOperationException("User already exists");

        var newUser = new VoidUser(Guid.NewGuid(), email, password.HashPassword());
        await _store.Set(newUser);
        return newUser;
    }
}
