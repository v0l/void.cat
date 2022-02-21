using VoidCat.Services.Abstractions;

namespace VoidCat.Model
{
    public class VoidSettings
    {
        public string DataDirectory { get; init; } = "./data";
        
        public TorSettings? TorSettings { get; init; }

        public JwtSettings JwtSettings { get; init; } = new("void_cat_internal", "default_key");
        
        public string? Redis { get; init; }
        
        public StrikeApiSettings? Strike { get; init; }
    }

    public sealed record TorSettings(Uri TorControl, string PrivateKey, string ControlPassword);

    public sealed record JwtSettings(string Issuer, string Key);
}
