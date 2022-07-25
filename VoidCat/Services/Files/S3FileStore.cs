using Amazon.S3;
using Amazon.S3.Model;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

/// <inheritdoc cref="VoidCat.Services.Abstractions.IFileStore" />
public class S3FileStore : StreamFileStore, IFileStore
{
    private readonly IFileInfoManager _fileInfo;
    private readonly AmazonS3Client _client;
    private readonly S3BlobConfig _config;
    private readonly IAggregateStatsCollector _statsCollector;

    public S3FileStore(S3BlobConfig settings, IAggregateStatsCollector stats, IFileInfoManager fileInfo) : base(stats)
    {
        _fileInfo = fileInfo;
        _statsCollector = stats;
        _config = settings;
        _client = _config.CreateClient();
    }

    /// <inheritdoc />
    public string Key => _config.Name;

    /// <inheritdoc />
    public async ValueTask<PrivateVoidFile> Ingress(IngressPayload payload, CancellationToken cts)
    {
        if (payload.IsAppend) throw new InvalidOperationException("Cannot append to S3 store");
        
        var req = new PutObjectRequest
        {
            BucketName = _config.BucketName,
            Key = payload.Id.ToString(),
            InputStream = payload.InStream,
            ContentType = payload.Meta.MimeType ?? "application/octet-stream",
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
                ContentLength = (long) payload.Meta.Size
            }
        };

        await _client.PutObjectAsync(req, cts);
        return HandleCompletedUpload(payload, payload.Meta.Size);
    }

    /// <inheritdoc />
    public async ValueTask Egress(EgressRequest request, Stream outStream, CancellationToken cts)
    {
        await using var stream = await Open(request, cts);
        await EgressFull(request.Id, stream, outStream, cts);
    }

    /// <inheritdoc />
    public ValueTask<EgressResult> StartEgress(EgressRequest request)
    {
        if (!_config.Direct) return ValueTask.FromResult(new EgressResult());

        var ub = new UriBuilder(_config.ServiceUrl!)
        {
            Path = $"/{_config.BucketName}/{request.Id}"
        };

        return ValueTask.FromResult(new EgressResult(ub.Uri));
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

    /// <inheritdoc />
    public async ValueTask DeleteFile(Guid id)
    {
        await _client.DeleteObjectAsync(_config.BucketName, id.ToString());
    }

    /// <inheritdoc />
    public async ValueTask<Stream> Open(EgressRequest request, CancellationToken cts)
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
        return obj.ResponseStream;
    }
}