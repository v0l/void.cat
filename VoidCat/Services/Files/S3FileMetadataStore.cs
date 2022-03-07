using Amazon.S3;
using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

public class S3FileMetadataStore : IFileMetadataStore
{
    private readonly ILogger<S3FileMetadataStore> _logger;
    private readonly AmazonS3Client _client;
    private readonly S3BlobConfig _config;
    private readonly bool _includeUrl;

    public S3FileMetadataStore(VoidSettings settings, ILogger<S3FileMetadataStore> logger)
    {
        _logger = logger;
        _includeUrl = settings.CloudStorage?.ServeFromCloud ?? false;
        _config = settings.CloudStorage!.S3!;
        _client = _config.CreateClient();
    }

    public ValueTask<TMeta?> Get<TMeta>(Guid id) where TMeta : VoidFileMeta
    {
        return GetMeta<TMeta>(id);
    }

    public ValueTask<VoidFileMeta?> Get(Guid id)
    {
        return GetMeta<VoidFileMeta>(id);
    }

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
            if (ret != default && _includeUrl)
            {
                var ub = new UriBuilder(_config.ServiceUrl!)
                {
                    Path = $"/{_config.BucketName}/{id}"
                };

                ret.Url = ub.Uri;
            }

            return ret;
        }
        catch (AmazonS3Exception aex)
        {
            _logger.LogError(aex, "Failed to get metadata for {Id}, {Error}", id, aex.Message);
        }

        return default;
    }
    
    private static string ToKey(Guid id) => $"{id}-metadata";
}
