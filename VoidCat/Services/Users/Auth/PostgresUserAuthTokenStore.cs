using Microsoft.EntityFrameworkCore;
using VoidCat.Database;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users.Auth;

/// <inheritdoc />
public class PostgresUserAuthTokenStore : IUserAuthTokenStore
{
    private readonly VoidContext _db;

    public PostgresUserAuthTokenStore(VoidContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async ValueTask<UserAuthToken?> Get(Guid id)
    {
        return await _db.UserAuthTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc />
    public async ValueTask Add(Guid id, UserAuthToken obj)
    {
        _db.UserAuthTokens.Add(obj);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await _db.UserAuthTokens
            .Where(a => a.Id == id)
            .ExecuteDeleteAsync();
    }
}
