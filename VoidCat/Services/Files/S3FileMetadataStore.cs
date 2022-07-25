using Amazon.S3;
using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

/// <inheritdoc />
public class S3FileMetadataStore : IFileMetadataStore
{
    private readonly ILogger<S3FileMetadataStore> _logger;
    private readonly AmazonS3Client _client;
    private readonly S3BlobConfig _config;

    public S3FileMetadataStore(S3BlobConfig settings, ILogger<S3FileMetadataStore> logger)
    {
        _logger = logger;
        _config = settings;
        _client = _config.CreateClient();
    }

    /// <inheritdoc />
    public string? Key => _config.Name;
    
    /// <inheritdoc />
    public ValueTask<TMeta?> Get<TMeta>(Guid id) where TMeta : VoidFileMeta
    {
        return GetMeta<TMeta>(id);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<TMeta>> Get<TMeta>(Guid[] ids) where TMeta : VoidFileMeta
    {
        var ret = new List<TMeta>();
        foreach (var id in ids)
        {
            var r = await GetMeta<TMeta>(id);
            if (r != null)
            {
                ret.Add(r);
            }
        }

        return ret;
    }

    /// <inheritdoc />
    public async ValueTask Update<TMeta>(Guid id, TMeta meta) where TMeta : VoidFileMeta
    {
        var oldMeta = await GetMeta<SecretVoidFileMeta>(id);
        if (oldMeta == default) return;

        oldMeta.Description = meta.Description ?? oldMeta.Description;
        oldMeta.Name = meta.Name ?? oldMeta.Name;
        oldMeta.MimeType = meta.MimeType ?? oldMeta.MimeType;
        oldMeta.Expires = meta.Expires ?? oldMeta.Expires;
        oldMeta.Storage = meta.Storage ?? oldMeta.Storage;

        await Set(id, oldMeta);
    }

    /// <inheritdoc />
    public ValueTask<PagedResult<TMeta>> ListFiles<TMeta>(PagedRequest request) where TMeta : VoidFileMeta
    {
        async IAsyncEnumerable<TMeta> Enumerate()
        {
            var obj = await _client.ListObjectsV2Async(new()
            {
                BucketName = _config.BucketName,
                Prefix = "metadata_",
                MaxKeys = 5_000
            });

            foreach (var file in obj.S3Objects)
            {
                if (Guid.TryParse(file.Key.Split("metadata_")[1], out var id))
                {
                    var meta = await GetMeta<TMeta>(id);
                    if (meta != default)
                    {
                        yield return meta;
                    }
                }
            }
        }

        return ValueTask.FromResult(new PagedResult<TMeta>
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Results = Enumerate().Skip(request.PageSize * request.Page).Take(request.PageSize)
        });
    }

    /// <inheritdoc />
    public async ValueTask<IFileMetadataStore.StoreStats> Stats()
    {
        var files = await ListFiles<VoidFileMeta>(new(0, Int32.MaxValue));
        var count = await files.Results.CountAsync();
        var size = await files.Results.SumAsync(a => (long) a.Size);
        return new(count, (ulong) size);
    }

    /// <inheritdoc />
    public ValueTask<VoidFileMeta?> Get(Guid id)
    {
        return GetMeta<VoidFileMeta>(id);
    }

    /// <inheritdoc />
    public ValueTask<SecretVoidFileMeta?> GetPrivate(Guid id)
    {
        return GetMeta<SecretVoidFileMeta>(id);
    }

    /// <inheritdoc />
    public async ValueTask Set(Guid id, SecretVoidFileMeta meta)
    {
        await _client.PutObjectAsync(new()
        {
            BucketName = _config.BucketName,
            Key = ToKey(id),
            ContentBody = JsonConvert.SerializeObject(meta),
            ContentType = "application/json"
        });
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await _client.DeleteObjectAsync(_config.BucketName, ToKey(id));
    }

    private async ValueTask<TMeta?> GetMeta<TMeta>(Guid id) where TMeta : VoidFileMeta
    {
        try
        {
            var obj = await _client.GetObjectAsync(_config.BucketName, ToKey(id));

            using var sr = new StreamReader(obj.ResponseStream);
            var json = await sr.ReadToEndAsync();
            var ret = JsonConvert.DeserializeObject<TMeta>(json);
            if (ret != default)
            {
                ret.Id = id;
            }

            return ret;
        }
        catch (AmazonS3Exception aex)
        {
            _logger.LogError(aex, "Failed to get metadata for {Id}, {Error}", id, aex.Message);
        }

        return default;
    }

    private static string ToKey(Guid id) => $"metadata_{id}";
}