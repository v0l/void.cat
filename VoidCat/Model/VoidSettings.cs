using VoidCat.Services.Strike;

namespace VoidCat.Model
{
    public class VoidSettings
    {
        public string DataDirectory { get; init; } = "./data";

        public TorSettings? TorSettings { get; init; }

        public JwtSettings JwtSettings { get; init; } = new()
        {
            Issuer = "void_cat_internal",
            Key = "default_key_void_cat_host"
        };

        public string? Redis { get; init; }

        public StrikeApiSettings? Strike { get; init; }

        public SmtpSettings? Smtp { get; init; }

        public List<Uri> CorsOrigins { get; init; } = new();

        public CloudStorageSettings? CloudStorage { get; init; }

        public VirusScannerSettings? VirusScanner { get; init; }
        
        public IEnumerable<string>? RequestHeadersLog { get; init; }
        
        public CaptchaSettings? CaptchaSettings { get; init; }
    }

    public sealed class TorSettings
    {
        public Uri TorControl { get; init; }
        public string PrivateKey { get; init; }
        public string ControlPassword { get; init; }
    }

    public sealed class JwtSettings
    {
        public string Issuer { get; init; }
        public string Key { get; init; }
    }

    public sealed class SmtpSettings
    {
        public Uri? Server { get; init; }
        public string? Username { get; init; }
        public string? Password { get; init; }
    }

    public sealed class CloudStorageSettings
    {
        public bool ServeFromCloud { get; init; }
        public S3BlobConfig? S3 { get; set; }
    }

    public sealed class S3BlobConfig
    {
        public string? AccessKey { get; init; }
        public string? SecretKey { get; init; }
        public Uri? ServiceUrl { get; init; }
        public string? Region { get; init; }
        public string? BucketName { get; init; } = "void-cat";
    }

    public sealed class VirusScannerSettings
    {
        public ClamAVSettings? ClamAV { get; init; }
        public VirusTotalConfig? VirusTotal { get; init; }
    }

    public sealed class ClamAVSettings
    {
        public Uri? Endpoint { get; init; }
        public long? MaxStreamSize { get; init; }
    }

    public sealed class VirusTotalConfig
    {
        public string? ApiKey { get; init; }
    }

    public sealed class CaptchaSettings
    {
        public string? SiteKey { get; init; }
        public string? Secret { get; init; }
    }
}