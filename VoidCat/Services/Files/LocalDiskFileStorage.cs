using VoidCat.Model;
using VoidCat.Model.Exceptions;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

public class LocalDiskFileStore : StreamFileStore, IFileStore
{
    private const string FilesDir = "files-v1";
    private readonly ILogger<LocalDiskFileStore> _logger;
    private readonly VoidSettings _settings;
    private readonly IFileMetadataStore _metadataStore;
    private readonly IFileInfoManager _fileInfo;

    public LocalDiskFileStore(ILogger<LocalDiskFileStore> logger, VoidSettings settings, IAggregateStatsCollector stats,
        IFileMetadataStore metadataStore, IFileInfoManager fileInfo, IUserUploadsStore userUploads)
        : base(stats, metadataStore, userUploads)
    {
        _settings = settings;
        _metadataStore = metadataStore;
        _fileInfo = fileInfo;
        _logger = logger;

        var dir = Path.Combine(_settings.DataDirectory, FilesDir);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public async ValueTask Egress(EgressRequest request, Stream outStream, CancellationToken cts)
    {
        var path = MapPath(request.Id);
        if (!File.Exists(path)) throw new VoidFileNotFoundException(request.Id);

        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        await EgressFromStream(fs, request, outStream, cts);
    }

    public async ValueTask<PrivateVoidFile> Ingress(IngressPayload payload, CancellationToken cts)
    {
        var fPath = MapPath(payload.Id);
        await using var fsTemp = new FileStream(fPath,
            payload.IsAppend ? FileMode.Append : FileMode.Create, FileAccess.Write);
        return await IngressToStream(fsTemp, payload, cts);
    }

    public ValueTask<PagedResult<PublicVoidFile>> ListFiles(PagedRequest request)
    {
        var files = Directory.EnumerateFiles(Path.Combine(_settings.DataDirectory, FilesDir))
            .Where(a => !Path.HasExtension(a));

        files = (request.SortBy, request.SortOrder) switch
        {
            (PagedSortBy.Id, PageSortOrder.Asc) => files.OrderBy(a =>
                Guid.TryParse(Path.GetFileNameWithoutExtension(a), out var g) ? g : Guid.Empty),
            (PagedSortBy.Id, PageSortOrder.Dsc) => files.OrderByDescending(a =>
                Guid.TryParse(Path.GetFileNameWithoutExtension(a), out var g) ? g : Guid.Empty),
            (PagedSortBy.Name, PageSortOrder.Asc) => files.OrderBy(Path.GetFileNameWithoutExtension),
            (PagedSortBy.Name, PageSortOrder.Dsc) => files.OrderByDescending(Path.GetFileNameWithoutExtension),
            (PagedSortBy.Size, PageSortOrder.Asc) => files.OrderBy(a => new FileInfo(a).Length),
            (PagedSortBy.Size, PageSortOrder.Dsc) => files.OrderByDescending(a => new FileInfo(a).Length),
            (PagedSortBy.Date, PageSortOrder.Asc) => files.OrderBy(File.GetCreationTimeUtc),
            (PagedSortBy.Date, PageSortOrder.Dsc) => files.OrderByDescending(File.GetCreationTimeUtc),
            _ => files
        };

        async IAsyncEnumerable<PublicVoidFile> EnumeratePage(IEnumerable<string> page)
        {
            foreach (var file in page)
            {
                if (!Guid.TryParse(Path.GetFileNameWithoutExtension(file), out var gid)) continue;

                var loaded = await _fileInfo.Get(gid);
                if (loaded != default)
                {
                    yield return loaded;
                }
            }
        }

        return ValueTask.FromResult(new PagedResult<PublicVoidFile>()
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalResults = files.Count(),
            Results = EnumeratePage(files.Skip(request.PageSize * request.Page).Take(request.PageSize))
        });
    }

    public async ValueTask DeleteFile(Guid id)
    {
        var fp = MapPath(id);
        if (File.Exists(fp))
        {
            _logger.LogInformation("Deleting file: {Path}", fp);
            File.Delete(fp);
        }

        await _metadataStore.Delete(id);
    }

    private string MapPath(Guid id) =>
        Path.Join(_settings.DataDirectory, FilesDir, id.ToString());
}