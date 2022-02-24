using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

public class UserManager : IUserManager
{
    private readonly IUserStore _store;
    private static bool _checkFirstRegister;

    public UserManager(IUserStore store)
    {
        _store = store;
    }

    public async ValueTask<VoidUser> Login(string email, string password)
    {
        var userId = await _store.LookupUser(email);
        if (!userId.HasValue) throw new InvalidOperationException("User does not exist");

        var user = await _store.Get<PrivateVoidUser>(userId.Value);
        if (!(user?.CheckPassword(password) ?? false)) throw new InvalidOperationException("User does not exist");

        user.LastLogin = DateTimeOffset.UtcNow;
        await _store.Set(user);

        return user;
    }

    public async ValueTask<VoidUser> Register(string email, string password)
    {
        var existingUser = await _store.LookupUser(email);
        if (existingUser != Guid.Empty) throw new InvalidOperationException("User already exists");

        var newUser = new PrivateVoidUser(Guid.NewGuid(), email, password.HashPassword())
        {
            Created = DateTimeOffset.UtcNow,
            LastLogin = DateTimeOffset.UtcNow
        };

        // automatically set first user to admin
        if (!_checkFirstRegister)
        {
            _checkFirstRegister = true;
            var users = await _store.ListUsers(new(0, 1));
            if (users.TotalResults == 0)
            {
                newUser.Roles.Add(Roles.Admin);
            }
        }
        
        await _store.Set(newUser);
        return newUser;
    }
}
