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
    }

    public sealed record TorSettings(Uri TorControl, string PrivateKey, string ControlPassword);

    public sealed record JwtSettings(string Issuer, string Key);

    public sealed record SmtpSettings(string Address, string Username, string Password);
}
