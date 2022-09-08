using VoidCat.Model.User;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// User access token store
/// </summary>
public interface IUserAuthTokenStore : IBasicStore<UserAuthToken>
{
}