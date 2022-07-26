using Newtonsoft.Json;

namespace VoidCat.Model;

public sealed class ApiKey
{
    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid Id { get; init; }
    
    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid UserId { get; init; }
    
    public string Token { get; init; }
    
    public DateTime Expiry { get; init; }
    
    public DateTime Created { get; init; }
}
