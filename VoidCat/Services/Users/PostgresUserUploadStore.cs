using Microsoft.EntityFrameworkCore;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

/// <inheritdoc />
public class PostgresUserUploadStore : IUserUploadsStore
{
    private readonly VoidContext _db;
    private readonly IServiceScopeFactory _scopeFactory;

    public PostgresUserUploadStore(VoidContext db, IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _scopeFactory = scopeFactory;
    }

    public async ValueTask<PagedResult<Guid>> ListFiles(Guid user, PagedRequest request)
    {
        var count = await _db.UserFiles.Where(a => a.UserId == user).CountAsync();

        async IAsyncEnumerable<Guid> EnumerateFiles()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoidContext>();
            var q = db.UserFiles
                .AsNoTracking()
                .Include(a => a.File)
                .Where(a => a.UserId == user)
                .AsQueryable();

            switch (request.SortBy, request.SortOrder)
            {
                case (PagedSortBy.Id, PageSortOrder.Asc):
                    q = q.OrderBy(a => a.FileId);
                    break;
                case (PagedSortBy.Id, PageSortOrder.Dsc):
                    q = q.OrderByDescending(a => a.FileId);
                    break;
                case (PagedSortBy.Name, PageSortOrder.Asc):
                    q = q.OrderBy(a => a.File.Name);
                    break;
                case (PagedSortBy.Name, PageSortOrder.Dsc):
                    q = q.OrderByDescending(a => a.File.Name);
                    break;
                case (PagedSortBy.Date, PageSortOrder.Asc):
                    q = q.OrderBy(a => a.File.Uploaded);
                    break;
                case (PagedSortBy.Date, PageSortOrder.Dsc):
                    q = q.OrderByDescending(a => a.File.Uploaded);
                    break;
                case (PagedSortBy.Size, PageSortOrder.Asc):
                    q = q.OrderBy(a => a.File.Size);
                    break;
                case (PagedSortBy.Size, PageSortOrder.Dsc):
                    q = q.OrderByDescending(a => a.File.Size);
                    break;
            }

            await foreach (var r in q.Skip(request.Page * request.PageSize)
                               .Take(request.PageSize)
                               .Select(a => a.FileId)
                               .AsAsyncEnumerable())
            {
                yield return r;
            }
        }

        return new()
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalResults = count,
            Data = EnumerateFiles()
        };
    }

    /// <inheritdoc />
    public async ValueTask AddFile(Guid user, Guid file)
    {
        _db.UserFiles.Add(new()
        {
            UserId = user,
            FileId = file
        });

        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async ValueTask<Guid?> Uploader(Guid file)
    {
        return await _db.UserFiles
            .Where(a => a.FileId == file)
            .Select(a => a.UserId)
            .SingleOrDefaultAsync();
    }
}
