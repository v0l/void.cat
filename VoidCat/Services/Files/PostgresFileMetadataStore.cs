﻿using Microsoft.EntityFrameworkCore;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

/// <inheritdoc />
public class PostgresFileMetadataStore : IFileMetadataStore
{
    private readonly VoidContext _db;
    private readonly IServiceScopeFactory _scopeFactory;

    public PostgresFileMetadataStore(VoidContext db, IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _scopeFactory = scopeFactory;
    }

    public string? Key => "postgres";

    /// <inheritdoc />
    public async ValueTask<Database.File?> Get(Guid id)
    {
        return await _db.Files
            .AsNoTracking()
            .Include(a => a.Paywall)
            .SingleOrDefaultAsync(a => a.Id == id);
    }

    public async ValueTask Add(Database.File f)
    {
        _db.Files.Add(f);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await _db.Files
            .Where(a => a.Id == id)
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<Database.File>> Get(Guid[] ids)
    {
        return await _db.Files
            .Include(a => a.Paywall)
            .Where(a => ids.Contains(a.Id))
            .ToArrayAsync();
    }

    /// <inheritdoc />
    public async ValueTask Update(Guid id, Database.File obj)
    {
        var existing = await _db.Files.FindAsync(id);
        if (existing == default)
        {
            return;
        }

        existing.Patch(obj);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async ValueTask<PagedResult<Database.File>> ListFiles(PagedRequest request)
    {
        IQueryable<Database.File> MakeQuery(VoidContext db)
        {
            var q = db.Files.AsNoTracking().AsQueryable();
            switch (request.SortBy, request.SortOrder)
            {
                case (PagedSortBy.Id, PageSortOrder.Asc):
                    q = q.OrderBy(a => a.Id);
                    break;
                case (PagedSortBy.Id, PageSortOrder.Dsc):
                    q = q.OrderByDescending(a => a.Id);
                    break;
                case (PagedSortBy.Name, PageSortOrder.Asc):
                    q = q.OrderBy(a => a.Name);
                    break;
                case (PagedSortBy.Name, PageSortOrder.Dsc):
                    q = q.OrderByDescending(a => a.Name);
                    break;
                case (PagedSortBy.Date, PageSortOrder.Asc):
                    q = q.OrderBy(a => a.Uploaded);
                    break;
                case (PagedSortBy.Date, PageSortOrder.Dsc):
                    q = q.OrderByDescending(a => a.Uploaded);
                    break;
                case (PagedSortBy.Size, PageSortOrder.Asc):
                    q = q.OrderBy(a => a.Size);
                    break;
                case (PagedSortBy.Size, PageSortOrder.Dsc):
                    q = q.OrderByDescending(a => a.Size);
                    break;
            }

            return q.Skip(request.Page * request.PageSize).Take(request.PageSize);
        }
        
        async IAsyncEnumerable<Database.File> Enumerate()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoidContext>();
            
            await foreach (var r in MakeQuery(db).AsAsyncEnumerable())
            {
                yield return r;
            }
        }

        return new()
        {
            TotalResults = await _db.Files.CountAsync(),
            Results = await MakeQuery(_db).CountAsync(),
            PageSize = request.PageSize,
            Page = request.Page,
            Data = Enumerate()
        };
    }

    /// <inheritdoc />
    public async ValueTask<IFileMetadataStore.StoreStats> Stats()
    {
        var size = await _db.Files
            .AsNoTracking()
            .SumAsync(a => (long)a.Size);

        var count = await _db.Files.CountAsync();
        return new(count, (ulong)size);
    }
}
