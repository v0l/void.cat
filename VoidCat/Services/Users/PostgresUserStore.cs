using Microsoft.EntityFrameworkCore;
using VoidCat.Database;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

/// <inheritdoc />
public class PostgresUserStore : IUserStore
{
    private readonly VoidContext _db;
    private readonly IServiceScopeFactory _scopeFactory;

    public PostgresUserStore(VoidContext db, IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async ValueTask<User?> Get(Guid id)
    {
        return await _db.Users
            .AsNoTracking()
            .Include(a => a.Roles)
            .SingleOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc />
    public async ValueTask Add(User obj)
    {
        _db.Users.Add(obj);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await _db.Users
            .Where(a => a.Id == id)
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public async ValueTask<Guid?> LookupUser(string email)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(a => a.Email == email)
            .Select(a => a.Id)
            .SingleOrDefaultAsync();
    }

    /// <inheritdoc />
    public async ValueTask<PagedResult<User>> ListUsers(PagedRequest request)
    {
        var totalUsers = await _db.Users.CountAsync();

        async IAsyncEnumerable<User> Enumerate()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoidContext>();
            var q = db.Users.AsNoTracking().AsQueryable();
            switch (request.SortBy, request.SortOrder)
            {
                case (PagedSortBy.Id, PageSortOrder.Asc):
                    q = q.OrderBy(a => a.Id);
                    break;
                case (PagedSortBy.Id, PageSortOrder.Dsc):
                    q = q.OrderByDescending(a => a.Id);
                    break;
                case (PagedSortBy.Name, PageSortOrder.Asc):
                    q = q.OrderBy(a => a.DisplayName);
                    break;
                case (PagedSortBy.Name, PageSortOrder.Dsc):
                    q = q.OrderByDescending(a => a.DisplayName);
                    break;
                case (PagedSortBy.Date, PageSortOrder.Asc):
                    q = q.OrderBy(a => a.Created);
                    break;
                case (PagedSortBy.Date, PageSortOrder.Dsc):
                    q = q.OrderByDescending(a => a.Created);
                    break;
            }

            await foreach (var r in q.Skip(request.Page * request.PageSize).Take(request.PageSize).AsAsyncEnumerable())
            {
                yield return r;
            }
        }

        return new()
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalResults = totalUsers,
            Data = Enumerate()
        };
    }

    /// <inheritdoc />
    public async ValueTask UpdateProfile(User newUser)
    {
        var u = await _db.Users
            .FindAsync(newUser.Id);

        if (u == null) return;

        var emailFlag = u.Flags.HasFlag(UserFlags.EmailVerified) ? UserFlags.EmailVerified : 0;
        u.DisplayName = newUser.DisplayName;
        u.Avatar = newUser.Avatar;
        u.Flags = newUser.Flags | emailFlag;
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async ValueTask UpdateLastLogin(Guid id, DateTime timestamp)
    {
        var u = await _db.Users
            .FindAsync(id);

        if (u == null) return;

        u.LastLogin = timestamp;
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async ValueTask AdminUpdateUser(User user)
    {
        var u = await _db.Users
            .FindAsync(user.Id);

        if (u == null) return;

        u.Email = user.Email;
        u.Storage = user.Storage;
        await _db.SaveChangesAsync();
    }
}
