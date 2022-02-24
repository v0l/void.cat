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

    public async ValueTask<T?> Get<T>(Guid id) where T : VoidUser
    {
        return await _cache.Get<T>(MapKey(id));
    }

    public async ValueTask Set(PrivateVoidUser user)
    {
        await _cache.Set(MapKey(user.Id), user);
        await _cache.AddToList(UserList, user.Id.ToString());
        await _cache.Set(MapKey(user.Email), user.Id.ToString());
    }

    public async ValueTask<PagedResult<PublicVoidUser>> ListUsers(PagedRequest request)
    {
        var users = (await _cache.GetList(UserList))?.Select(Guid.Parse);
        users = (request.SortBy, request.SortOrder) switch
        {
            (PagedSortBy.Id, PageSortOrder.Asc) => users?.OrderBy(a => a),
            (PagedSortBy.Id, PageSortOrder.Dsc) => users?.OrderByDescending(a => a),
            _ => users
        };

        async IAsyncEnumerable<PublicVoidUser> EnumerateUsers(IEnumerable<Guid> ids)
        {
            var usersLoaded = await Task.WhenAll(ids.Select(async a => await Get<PublicVoidUser>(a)));
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

    private static string MapKey(Guid id) => $"user:{id}";
    private static string MapKey(string email) => $"user:email:{email}";
}
