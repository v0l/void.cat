using Newtonsoft.Json;
using VoidCat.Database;

namespace VoidCat.Model;

/// <summary>
/// A user object which can be returned via the API
/// </summary>
public class ApiUser
{
    /// <summary>
    /// Unique Id of the user
    /// </summary>
    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid Id { get; init; }
    
    /// <summary>
    /// Display name
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// Avatar
    /// </summary>
    public string? Avatar { get; init; }
    
    /// <summary>
    /// If profile can be viewed by anyone
    /// </summary>
    public bool PublicProfile { get; init; }
    
    /// <summary>
    /// If the users uploads can be viewed by anyone
    /// </summary>
    public bool PublicUploads { get; init; }
    
    /// <summary>
    /// If the account is not email verified
    /// </summary>
    public bool? NeedsVerification { get; init; }

    /// <summary>
    /// A list of roles the user has
    /// </summary>
    public List<string> Roles { get; init; } = new();
    
    /// <summary>
    /// When the account was created
    /// </summary>
    public DateTime Created { get; init; }
    
    public bool IsNostr { get; init; }
}
