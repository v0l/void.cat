using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

public class UserUploadStore : IUserUploadsStore
{
    private readonly ICache _cache;
    private readonly IFileInfoManager _fileInfo;

    public UserUploadStore(ICache cache, IFileInfoManager fileInfo)
    {
        _cache = cache;
        _fileInfo = fileInfo;
    }

    public async ValueTask<PagedResult<PublicVoidFile>> ListFiles(Guid user, PagedRequest request)
    {
        var ids = (await _cache.GetList(MapKey(user))).Select(Guid.Parse);
        ids = (request.SortBy, request.SortOrder) switch
        {
            (PagedSortBy.Id, PageSortOrder.Asc) => ids.OrderBy(a => a),
            (PagedSortBy.Id, PageSortOrder.Dsc) => ids.OrderByDescending(a => a),
            _ => ids
        };

        async IAsyncEnumerable<PublicVoidFile> EnumerateResults(IEnumerable<Guid> page)
        {
            foreach (var guid in page)
            {
                var info = await _fileInfo.Get(guid);
                if (info != default)
                {
                    yield return info;
                }
            }
        }

        return new()
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalResults = ids?.Count() ?? 0,
            Results = EnumerateResults(ids.Skip(request.Page * request.PageSize).Take(request.PageSize))
        };
    }

    public ValueTask AddFile(Guid user, PrivateVoidFile voidFile)
    {
        return _cache.AddToList(MapKey(user), voidFile.Id.ToString());
    }

    private static string MapKey(Guid id) => $"user:{id}:uploads";
}
