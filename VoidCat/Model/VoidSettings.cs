using VoidCat.Services.Strike;

namespace VoidCat.Model
{
    /// <summary>
    /// System settings
    /// </summary>
    public class VoidSettings
    {
        /// <summary>
        /// Data directory to store files in
        /// </summary>
        public string DataDirectory { get; init; } = "./data";

        /// <summary>
        /// Tor configuration
        /// </summary>
        public TorSettings? TorSettings { get; init; }

        /// <summary>
        /// JWT settings for login token signing
        /// </summary>
        public JwtSettings JwtSettings { get; init; } = new()
        {
            Issuer = "void_cat_internal",
            Key = "default_key_void_cat_host"
        };

        /// <summary>
        /// Redis database connection string
        /// </summary>
        public string? Redis { get; init; }

        /// <summary>
        /// Strike payment service api settings
        /// </summary>
        public StrikeApiSettings? Strike { get; init; }

        /// <summary>
        /// Email server settings
        /// </summary>
        public SmtpSettings? Smtp { get; init; }

        /// <summary>
        /// CORS origins
        /// </summary>
        public List<Uri> CorsOrigins { get; init; } = new();

        /// <summary>
        /// Cloud file storage settings
        /// </summary>
        public CloudStorageSettings? CloudStorage { get; init; }

        /// <summary>
        /// Virus scanner settings
        /// </summary>
        public VirusScannerSettings? VirusScanner { get; init; }
        
        /// <summary>
        /// Request header to unmask in the logs, otherwise all are masked
        /// </summary>
        public IEnumerable<string>? RequestHeadersLog { get; init; }
        
        /// <summary>
        /// hCaptcha settings
        /// </summary>
        public CaptchaSettings? CaptchaSettings { get; init; }
        
        /// <summary>
        /// Postgres database connection string
        /// </summary>
        public string? Postgres { get; init; }
        
        /// <summary>
        /// Prometheus server for querying metrics
        /// </summary>
        public Uri? Prometheus { get; init; }
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