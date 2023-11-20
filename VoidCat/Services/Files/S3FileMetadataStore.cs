using Amazon.S3;
using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Services.Abstractions;
using File = VoidCat.Database.File;

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

    public string? Key => _config.Name;
    
    /// <inheritdoc />
    public async ValueTask<Database.File?> Get(Guid id)
    {
        try
        {
            var obj = await _client.GetObjectAsync(_config.BucketName, ToKey(id));

            using var sr = new StreamReader(obj.ResponseStream);
            var json = await sr.ReadToEndAsync();
            var ret = JsonConvert.DeserializeObject<Database.File>(json);
            return ret;
        }
        catch (AmazonS3Exception aex)
        {
            _logger.LogError(aex, "Failed to get metadata for {Id}, {Error}", id, aex.Message);
        }

        return default;
    }
    
    public ValueTask<File?> GetHash(string digest)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<Database.File>> Get(Guid[] ids)
    {
        var ret = new List<Database.File>();
        foreach (var id in ids)
        {
            var r = await Get(id);
            if (r != null)
            {
                ret.Add(r);
            }
        }

        return ret;
    }
    public ValueTask Add(Database.File f)
    {
        return Set(f.Id, f);
    }

    /// <inheritdoc />
    public async ValueTask Update(Guid id, Database.File meta)
    {
        var oldMeta = await Get(id);
        if (oldMeta == default) return;

        oldMeta.Patch(meta);
        await Set(id, oldMeta);
    }

    /// <inheritdoc />
    public ValueTask<PagedResult<Database.File>> ListFiles(PagedRequest request)
    {
        async IAsyncEnumerable<Database.File> Enumerate()
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
                    var meta = await Get(id);
                    if (meta != default)
                    {
                        yield return meta;
                    }
                }
            }
        }

        return ValueTask.FromResult(new PagedResult<Database.File>
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Data = Enumerate().Skip(request.PageSize * request.Page).Take(request.PageSize)
        });
    }

    /// <inheritdoc />
    public async ValueTask<IFileMetadataStore.StoreStats> Stats()
    {
        var files = await ListFiles(new(0, Int32.MaxValue));
        var count = await files.Data.CountAsync();
        var size = await files.Data.SumAsync(a => (long) a.Size);
        return new(count, (ulong) size);
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await _client.DeleteObjectAsync(_config.BucketName, ToKey(id));
    }

    private async ValueTask Set(Guid id, Database.File meta)
    {
        await _client.PutObjectAsync(new()
        {
            BucketName = _config.BucketName,
            Key = ToKey(id),
            ContentBody = JsonConvert.SerializeObject(meta),
            ContentType = "application/json"
        });
    }

    private static string ToKey(Guid id) => $"metadata_{id}";
}