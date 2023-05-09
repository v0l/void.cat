using Microsoft.EntityFrameworkCore;
using VoidCat.Database;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.VirusScanner;

/// <inheritdoc />
public class PostgresVirusScanStore : IVirusScanStore
{
    private readonly VoidContext _db;

    public PostgresVirusScanStore(VoidContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async ValueTask<VirusScanResult?> Get(Guid id)
    {
        return await _db.VirusScanResults
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc />
    public async ValueTask<VirusScanResult?> GetByFile(Guid id)
    {
        return await _db.VirusScanResults
            .AsNoTracking()
            .Where(a => a.FileId == id)
            .OrderByDescending(a => a.ScanTime)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async ValueTask Add(Guid id, VirusScanResult obj)
    {
        _db.VirusScanResults.Add(obj);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await _db.VirusScanResults
            .Where(a => a.Id == id)
            .ExecuteDeleteAsync();
    }
}
