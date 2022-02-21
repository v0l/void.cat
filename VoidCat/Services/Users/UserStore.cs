using System.Runtime.CompilerServices;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

public class UserStore : IUserStore
{
    private const string UserList = "users";
    private readonly ICache _cache;

    public UserStore(ICache cache)
    {
        _cache = cache;
    }

    public async ValueTask<Guid?> LookupUser(string email)
    {
        return await _cache.Get<Guid>(MapKey(email));
    }

    public async ValueTask<VoidUser?> Get(Guid id)
    {
        return await _cache.Get<VoidUser>(MapKey(id));
    }

    public async ValueTask Set(VoidUser user)
    {
        await _cache.Set(MapKey(user.Id), user);
        await _cache.AddToList(UserList, user.Id.ToString());
        await _cache.Set(MapKey(user.Email), user.Id.ToString());
    }

    public async IAsyncEnumerable<VoidUser> ListUsers([EnumeratorCancellation] CancellationToken cts = default)
    {
        var users = (await _cache.GetList(UserList))?.Select(Guid.Parse);
        if (users != default)
        {
            while (!cts.IsCancellationRequested)
            {
                var loadUsers = await Task.WhenAll(users.Select(async a => await Get(a)));
                foreach (var user in loadUsers)
                {
                    if (user != default)
                    {
                        yield return user;
                    }
                }
            }
        }
    }

    private static string MapKey(Guid id) => $"user:{id}";
    private static string MapKey(string email) => $"user:email:{email}";
}
