using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

public class UserStore : IUserStore
{
    private const string UserList = "users";
    private readonly ILogger<UserStore> _logger;
    private readonly ICache _cache;

    public UserStore(ICache cache, ILogger<UserStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async ValueTask<Guid?> LookupUser(string email)
    {
        return await _cache.Get<Guid>(MapKey(email));
    }

    public async ValueTask<VoidUser?> Get(Guid id)
    {
        return await Get<PublicVoidUser>(id);
    }

    public async ValueTask<T?> Get<T>(Guid id) where T : VoidUser
    {
        try
        {
            return await _cache.Get<T>(MapKey(id));
        }
        catch (FormatException)
        {
            _logger.LogWarning("Corrupt user data at: {Key}", MapKey(id));
        }

        return default;
    }

    public async ValueTask Set(Guid id, InternalVoidUser user)
    {
        if (id != user.Id) throw new InvalidOperationException();

        await _cache.Set(MapKey(user.Id), user);
        await _cache.AddToList(UserList, user.Id.ToString());
        await _cache.Set(MapKey(user.Email), user.Id.ToString());
    }

    public async ValueTask<PagedResult<PrivateVoidUser>> ListUsers(PagedRequest request)
    {
        var users = (await _cache.GetList(UserList))
            .Select<string, Guid?>(a => Guid.TryParse(a, out var g) ? g : null)
            .Where(a => a.HasValue).Select(a => a.Value);
        users = (request.SortBy, request.SortOrder) switch
        {
            (PagedSortBy.Id, PageSortOrder.Asc) => users?.OrderBy(a => a),
            (PagedSortBy.Id, PageSortOrder.Dsc) => users?.OrderByDescending(a => a),
            _ => users
        };

        async IAsyncEnumerable<PrivateVoidUser> EnumerateUsers(IEnumerable<Guid> ids)
        {
            var usersLoaded = await Task.WhenAll(ids.Select(async a => await Get<PrivateVoidUser>(a)));
            foreach (var user in usersLoaded)
            {
                if (user != default)
                {
                    yield return user;
                }
            }
        }

        return new()
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalResults = users?.Count() ?? 0,
            Results = EnumerateUsers(users?.Skip(request.PageSize * request.Page).Take(request.PageSize))
        };
    }

    public async ValueTask UpdateProfile(PublicVoidUser newUser)
    {
        var oldUser = await Get<InternalVoidUser>(newUser.Id);
        if (oldUser == null) return;

        //retain flags
        var isEmailVerified = oldUser.Flags.HasFlag(VoidUserFlags.EmailVerified);
        
        // update only a few props
        oldUser.Avatar = newUser.Avatar;
        oldUser.Flags = newUser.Flags | (isEmailVerified ? VoidUserFlags.EmailVerified : 0);
        oldUser.DisplayName = newUser.DisplayName;

        await Set(newUser.Id, oldUser);
    }

    public async ValueTask Delete(Guid id)
    {
        var user = await Get<InternalVoidUser>(id);
        if (user == default) throw new InvalidOperationException();
        await Delete(user);
    }

    public async ValueTask Delete(PrivateVoidUser user)
    {
        await _cache.Delete(MapKey(user.Id));
        await _cache.RemoveFromList(UserList, user.Id.ToString());
        await _cache.Delete(MapKey(user.Email));
    }

    private static string MapKey(Guid id) => $"user:{id}";
    private static string MapKey(string email) => $"user:email:{email.Hash("md5")}";
}