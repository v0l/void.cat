using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

/// <inheritdoc />
public class CacheUserStore : IUserStore
{
    private const string UserList = "users";
    private readonly ICache _cache;

    public CacheUserStore(ICache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async ValueTask<Guid?> LookupUser(string email)
    {
        return await _cache.Get<Guid>(MapKey(email));
    }

    /// <inheritdoc />
    public ValueTask<T?> Get<T>(Guid id) where T : VoidUser
    {
        return _cache.Get<T>(MapKey(id));
    }

    /// <inheritdoc />
    public ValueTask<VoidUser?> Get(Guid id)
    {
        return Get<VoidUser>(id);
    }

    /// <inheritdoc />
    public ValueTask<InternalVoidUser?> GetPrivate(Guid id)
    {
        return Get<InternalVoidUser>(id);
    }

    /// <inheritdoc />
    public async ValueTask Set(Guid id, InternalVoidUser user)
    {
        if (id != user.Id) throw new InvalidOperationException();

        await _cache.Set(MapKey(user.Id), user);
        await _cache.AddToList(UserList, user.Id.ToString());
        await _cache.Set(MapKey(user.Email), user.Id.ToString());
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async ValueTask UpdateLastLogin(Guid id, DateTime timestamp)
    {
        var user = await Get<InternalVoidUser>(id);
        if (user != default)
        {
            user.LastLogin = timestamp;
            await Set(user.Id, user);
        }
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        var user = await Get<InternalVoidUser>(id);
        if (user == default) throw new InvalidOperationException();
        await Delete(user);
    }

    private async ValueTask Delete(PrivateVoidUser user)
    {
        await _cache.Delete(MapKey(user.Id));
        await _cache.RemoveFromList(UserList, user.Id.ToString());
        await _cache.Delete(MapKey(user.Email));
    }

    private static string MapKey(Guid id) => $"user:{id}";
    private static string MapKey(string email) => $"user:email:{email.Hash("md5")}";
}