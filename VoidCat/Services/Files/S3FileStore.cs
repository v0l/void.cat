using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

/// <inheritdoc cref="VoidCat.Services.Abstractions.IFileStore" />
public class S3FileStore : StreamFileStore, IFileStore
{
    private readonly IFileMetadataStore _fileInfo;
    private readonly AmazonS3Client _client;
    private readonly S3BlobConfig _config;
    private readonly IAggregateStatsCollector _statsCollector;
    private readonly ICache _cache;

    public S3FileStore(S3BlobConfig settings, IAggregateStatsCollector stats, IFileMetadataStore fileInfo, ICache cache) : base(stats)
    {
        _fileInfo = fileInfo;
        _cache = cache;
        _statsCollector = stats;
        _config = settings;
        _client = _config.CreateClient();
    }

    public string Key => _config.Name;

    public async ValueTask<bool> Exists(Guid id)
    {
        try
        {
            await _client.GetObjectMetadataAsync(new GetObjectMetadataRequest()
            {
                BucketName = _config.BucketName,
                Key = id.ToString()
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask<Database.File> Ingress(IngressPayload payload, CancellationToken cts)
    {
        if (payload.IsMultipart) return await IngressMultipart(payload, cts);

        var req = new PutObjectRequest
        {
            BucketName = _config.BucketName,
            Key = payload.Id.ToString(),
            InputStream = payload.InStream,
            ContentType = "application/octet-stream",
            AutoResetStreamPosition = false,
            AutoCloseStream = false,
            ChecksumAlgorithm = _config.SendChecksum ? ChecksumAlgorithm.SHA256 : null,
            ChecksumSHA256 = payload.Meta.Digest != default && _config.SendChecksum ?
                Convert.ToBase64String(payload.Meta.Digest!.FromHex()) : null,
            DisablePayloadSigning = _config.DisablePayloadSigning,
            Headers =
            {
                ContentLength = (long)payload.Meta.Size
            }
        };

        await _client.PutObjectAsync(req, cts);
        await _statsCollector.TrackIngress(payload.Id, payload.Meta.Size);
        return HandleCompletedUpload(payload, payload.Meta.Size);
    }

    /// <inheritdoc />
    public async ValueTask Egress(EgressRequest request, Stream outStream, CancellationToken cts)
    {
        await using var stream = await Open(request, cts);
        await EgressFull(request.Id, stream, outStream, cts);
    }

    /// <inheritdoc />
    public async ValueTask<EgressResult> StartEgress(EgressRequest request)
    {
        if (!_config.Direct) return new();

        var meta = await _fileInfo.Get(request.Id);
        var url = _client.GetPreSignedURL(new()
        {
            BucketName = _config.BucketName,
            Expires = DateTime.UtcNow.AddHours(1),
            Key = request.Id.ToString(),
            ResponseHeaderOverrides = new()
            {
                ContentDisposition = $"inline; filename=\"{meta?.Name}\"",
                ContentType = meta?.MimeType
            }
        });

        return new(new Uri(url));
    }

    public async ValueTask<PagedResult<Database.File>> ListFiles(PagedRequest request)
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

            async IAsyncEnumerable<Database.File> EnumerateFiles(IEnumerable<S3Object> page)
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
                Results = AsyncEnumerable.Empty<Database.File>()
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

    private async Task<Database.File> IngressMultipart(IngressPayload payload, CancellationToken cts)
    {
        string? uploadId = null;
        var cacheKey = $"s3:{_config.Name}:multipart-upload-id:{payload.Id}";
        var partsCacheKey = $"s3:{_config.Name}:multipart-upload:{payload.Id}";

        try
        {
            if (payload.Segment == 1)
            {
                var mStart = new InitiateMultipartUploadRequest()
                {
                    BucketName = _config.BucketName,
                    Key = payload.Id.ToString(),
                    ContentType = "application/octet-stream",
                    ChecksumAlgorithm = _config.SendChecksum ? ChecksumAlgorithm.SHA256 : null
                };

                var mStartResult = await _client.InitiateMultipartUploadAsync(mStart, cts);
                uploadId = mStartResult.UploadId;
                await _cache.Set(cacheKey, uploadId, TimeSpan.FromHours(1));
            }
            else
            {
                uploadId = await _cache.Get<string>(cacheKey);
            }

            // sadly it seems like we need a tmp file here
            var tmpFile = Path.GetTempFileName();
            await using var fsTmp = new FileStream(tmpFile, FileMode.Create, FileAccess.ReadWrite);
            await payload.InStream.CopyToAsync(fsTmp, cts);
            fsTmp.Seek(0, SeekOrigin.Begin);

            var segmentLength = (ulong)fsTmp.Length;
            var mBody = new UploadPartRequest()
            {
                UploadId = uploadId,
                BucketName = _config.BucketName,
                PartNumber = payload.Segment,
                Key = payload.Id.ToString(),
                InputStream = fsTmp,
                DisablePayloadSigning = _config.DisablePayloadSigning
            };

            var bodyResponse = await _client.UploadPartAsync(mBody, cts);
            if (bodyResponse.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Upload aborted");
            }

            await _statsCollector.TrackIngress(payload.Id, segmentLength);
            await _cache.AddToList(partsCacheKey, $"{payload.Segment}|{bodyResponse.ETag.Replace("\"", string.Empty)}");
            if (payload.Segment == payload.TotalSegments)
            {
                var parts = await _cache.GetList(partsCacheKey);
                var completeResponse = await _client.CompleteMultipartUploadAsync(new()
                {
                    BucketName = _config.BucketName,
                    Key = payload.Id.ToString(),
                    UploadId = uploadId,
                    ChecksumSHA256 = payload.Meta.Digest != default && _config.SendChecksum ?
                        Convert.ToBase64String(payload.Meta.Digest!.FromHex()) : null,
                    PartETags = parts.Select(a =>
                    {
                        var pSplit = a.Split('|');
                        return new PartETag()
                        {
                            PartNumber = int.Parse(pSplit[0]),
                            ETag = pSplit[1]
                        };
                    }).ToList()
                }, cts);

                if (completeResponse.HttpStatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("Upload failed");
                }
            }


            return HandleCompletedUpload(payload, segmentLength);
        }
        catch
        {
            if (uploadId != null)
            {
                await _client.AbortMultipartUploadAsync(new()
                {
                    Key = payload.Id.ToString(),
                    BucketName = _config.BucketName,
                    UploadId = uploadId
                }, cts);
            }

            throw;
        }
    }
}
