using Amazon.S3;
using Amazon.S3.Model;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

public class S3FileStore : StreamFileStore, IFileStore
{
    private readonly IFileInfoManager _fileInfo;
    private readonly AmazonS3Client _client;
    private readonly S3BlobConfig _config;
    private readonly IAggregateStatsCollector _statsCollector;

    public S3FileStore(VoidSettings settings, IAggregateStatsCollector stats, IFileMetadataStore metadataStore,
        IUserUploadsStore userUploads, IFileInfoManager fileInfo) : base(stats, metadataStore, userUploads)
    {
        _fileInfo = fileInfo;
        _statsCollector = stats;
        _config = settings.CloudStorage!.S3!;
        _client = _config.CreateClient();
    }

    public async ValueTask<PrivateVoidFile> Ingress(IngressPayload payload, CancellationToken cts)
    {
        var req = new PutObjectRequest
        {
            BucketName = _config.BucketName,
            Key = payload.Id.ToString(),
            InputStream = payload.InStream,
            ContentType = "application/octet-stream",
            AutoResetStreamPosition = false,
            AutoCloseStream = false,
            ChecksumAlgorithm = ChecksumAlgorithm.SHA256,
            ChecksumSHA256 = payload.Hash != default ? Convert.ToBase64String(payload.Hash!.FromHex()) : null,
            StreamTransferProgress = (s, e) =>
            {
                _statsCollector.TrackIngress(payload.Id, (ulong) e.IncrementTransferred)
                    .GetAwaiter().GetResult();
            },
            Headers =
            {
                ContentLength = (long)payload.Meta.Size
            }
        };

        var r = await _client.PutObjectAsync(req, cts);
        return await HandleCompletedUpload(payload, r.ChecksumSHA256, payload.Meta.Size);
    }

    public async ValueTask Egress(EgressRequest request, Stream outStream, CancellationToken cts)
    {
        var req = new GetObjectRequest()
        {
            BucketName = _config.BucketName,
            Key = request.Id.ToString()
        };
        if (request.Ranges.Any())
        {
            var r = request.Ranges.First();
            req.ByteRange = new ByteRange(r.OriginalString);
        }

        var obj = await _client.GetObjectAsync(req, cts);
        await EgressFull(request.Id, obj.ResponseStream, outStream, cts);
    }

    public async ValueTask<PagedResult<PublicVoidFile>> ListFiles(PagedRequest request)
    {
        try
        {
            var objs = await _client.ListObjectsV2Async(new ListObjectsV2Request()
            {
                BucketName = _config.BucketName,
            });

            var files = (request.SortBy, request.SortOrder) switch
            {
                (PagedSortBy.Date, PageSortOrder.Asc) => objs.S3Objects.OrderBy(a => a.LastModified),
                (PagedSortBy.Date, PageSortOrder.Dsc) => objs.S3Objects.OrderByDescending(a => a.LastModified),
                (PagedSortBy.Name, PageSortOrder.Asc) => objs.S3Objects.OrderBy(a => a.Key),
                (PagedSortBy.Name, PageSortOrder.Dsc) => objs.S3Objects.OrderByDescending(a => a.Key),
                (PagedSortBy.Size, PageSortOrder.Asc) => objs.S3Objects.OrderBy(a => a.Size),
                (PagedSortBy.Size, PageSortOrder.Dsc) => objs.S3Objects.OrderByDescending(a => a.Size),
                _ => objs.S3Objects.AsEnumerable()
            };

            async IAsyncEnumerable<PublicVoidFile> EnumerateFiles(IEnumerable<S3Object> page)
            {
                foreach (var item in page)
                {
                    if (!Guid.TryParse(item.Key, out var gid)) continue;

                    var obj = await _fileInfo.Get(gid);
                    if (obj != default)
                    {
                        yield return obj;
                    }
                }
            }

            return new()
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalResults = files.Count(),
                Results = EnumerateFiles(files.Skip(request.PageSize * request.Page).Take(request.PageSize))
            };
        }
        catch (AmazonS3Exception aex)
        {
            // ignore
            return new()
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalResults = 0,
                Results = AsyncEnumerable.Empty<PublicVoidFile>()
            };
        }
    }

    public async ValueTask DeleteFile(Guid id)
    {
        await _client.DeleteObjectAsync(_config.BucketName, id.ToString());
    }
}