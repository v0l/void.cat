using Microsoft.EntityFrameworkCore;
using VoidCat.Database;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

/// <inheritdoc />
public class PostgresApiKeyStore : IApiKeyStore
{
    private readonly VoidContext _db;

    public PostgresApiKeyStore(VoidContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async ValueTask<ApiKey?> Get(Guid id)
    {
        return await _db.ApiKeys
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc />
    public async ValueTask Add(Guid id, ApiKey obj)
    {
        _db.ApiKeys.Add(obj);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await _db.ApiKeys
            .Where(a => a.Id == id)
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ApiKey>> ListKeys(Guid id)
    {
        return await _db.ApiKeys
            .AsNoTracking()
            .Where(a => a.UserId == id)
            .ToArrayAsync();
    }
}
