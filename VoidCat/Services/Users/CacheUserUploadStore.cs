using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

/// <inheritdoc />
public class CacheUserUploadStore : IUserUploadsStore
{
    private readonly ICache _cache;

    public CacheUserUploadStore(ICache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async ValueTask<PagedResult<Guid>> ListFiles(Guid user, PagedRequest request)
    {
        var ids = (await _cache.GetList(MapKey(user))).Select(Guid.Parse);
        ids = (request.SortBy, request.SortOrder) switch
        {
            (PagedSortBy.Id, PageSortOrder.Asc) => ids.OrderBy(a => a),
            (PagedSortBy.Id, PageSortOrder.Dsc) => ids.OrderByDescending(a => a),
            _ => ids
        };

        var idsRendered = ids.ToList();
        async IAsyncEnumerable<Guid> EnumerateResults(IEnumerable<Guid> page)
        {
            foreach (var id in page)
            {
                yield return id;
            }
        }

        return new()
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalResults = idsRendered.Count,
            Data = EnumerateResults(idsRendered.Skip(request.Page * request.PageSize).Take(request.PageSize))
        };
    }

    /// <inheritdoc />
    public async ValueTask AddFile(Guid user, Guid file)
    {
        await _cache.AddToList(MapKey(user), file.ToString());
        await _cache.Set(MapUploader(file), user);
    }

    /// <inheritdoc />
    public ValueTask<Guid?> Uploader(Guid file)
    {
        return _cache.Get<Guid?>(MapUploader(file));
    }

    private static string MapKey(Guid id) => $"user:{id}:uploads";
    private static string MapUploader(Guid file) => $"file:{file}:uploader";
}