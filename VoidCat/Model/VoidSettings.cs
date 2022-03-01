using VoidCat.Services.Strike;

namespace VoidCat.Model
{
    public class VoidSettings
    {
        public string DataDirectory { get; init; } = "./data";
        
        public TorSettings? TorSettings { get; init; }

        public JwtSettings JwtSettings { get; init; } = new("void_cat_internal", "default_key_void_cat_host");
        
        public string? Redis { get; init; }
        
        public StrikeApiSettings? Strike { get; init; }
        
        public SmtpSettings? Smtp { get; init; }

        public List<Uri> CorsOrigins { get; init; } = new();
        
        public CloudStorageSettings? CloudStorage { get; init; }
    }

    public sealed record TorSettings(Uri TorControl, string PrivateKey, string ControlPassword);

    public sealed record JwtSettings(string Issuer, string Key);

    public sealed record SmtpSettings
    {
        public Uri? Server { get; init; }
        public string? Username { get; init; }
        public string? Password { get; init; }
    }

    public sealed record CloudStorageSettings
    {
        public S3BlobConfig? S3 { get; set; }
    }

    public sealed record S3BlobConfig
    {
        public string? AccessKey { get; init; }
        public string? SecretKey { get; init; }
        public Uri? ServiceUrl { get; init; }
        public string? Region { get; init; }
        public string? BucketName { get; init; } = "void-cat";
    }
}
