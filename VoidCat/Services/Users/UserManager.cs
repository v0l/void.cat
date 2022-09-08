using VoidCat.Model;
using VoidCat.Model.User;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Users.Auth;

namespace VoidCat.Services.Users;

public class UserManager
{
    private readonly IUserStore _store;
    private readonly IEmailVerification _emailVerification;
    private readonly IUserAuthTokenStore _tokenStore;
    private readonly OAuthFactory _oAuthFactory;
    private static bool _checkFirstRegister;

    public UserManager(IUserStore store, IEmailVerification emailVerification, OAuthFactory oAuthFactory,
        IUserAuthTokenStore tokenStore)
    {
        _store = store;
        _emailVerification = emailVerification;
        _oAuthFactory = oAuthFactory;
        _tokenStore = tokenStore;
    }

    /// <summary>
    /// Login an existing user with email/password
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async ValueTask<InternalUser> Login(string email, string password)
    {
        var userId = await _store.LookupUser(email);
        if (!userId.HasValue) throw new InvalidOperationException("User does not exist");

        var user = await _store.GetPrivate(userId.Value);
        if (!(user?.CheckPassword(password) ?? false)) throw new InvalidOperationException("User does not exist");

        await HandleLogin(user);
        return user;
    }

    /// <summary>
    /// Register a new internal user with email/password
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async ValueTask<InternalUser> Register(string email, string password)
    {
        var existingUser = await _store.LookupUser(email);
        if (existingUser != Guid.Empty && existingUser != null)
            throw new InvalidOperationException("User already exists");

        var newUser = new InternalUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            Password = password.HashPassword(),
            Created = DateTimeOffset.UtcNow,
            LastLogin = DateTimeOffset.UtcNow
        };

        await SetupNewUser(newUser);
        return newUser;
    }

    /// <summary>
    /// Start OAuth2 authorization flow
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public Uri Authorize(string provider)
    {
        var px = _oAuthFactory.GetProvider(provider);
        return px.Authorize();
    }

    /// <summary>
    /// Login or Register with OAuth2 auth code
    /// </summary>
    /// <param name="code"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    public async ValueTask<InternalUser> LoginOrRegister(string code, string provider)
    {
        var px = _oAuthFactory.GetProvider(provider);
        var token = await px.GetToken(code);

        var user = await px.GetUserDetails(token);
        if (user == default)
        {
            throw new InvalidOperationException($"Could not load user profile from provider: {provider}");
        }

        var uid = await _store.LookupUser(user.Email);
        if (uid.HasValue)
        {
            var existingUser = await _store.GetPrivate(uid.Value);
            if (existingUser?.AuthType == AuthType.OAuth2)
            {
                return existingUser;
            }

            throw new InvalidOperationException("Auth failure, user type does not match!");
        }

        await SetupNewUser(user);
        return user;
    }

    private async Task SetupNewUser(InternalUser newUser)
    {
        // automatically set first user to admin
        if (!_checkFirstRegister)
        {
            _checkFirstRegister = true;
            var users = await _store.ListUsers(new(0, 1));
            if (users.TotalResults == 0)
            {
                newUser.Roles.Add(Roles.Admin);
            }
        }

        await _store.Set(newUser.Id, newUser);
        await _emailVerification.SendNewCode(newUser);
    }

    private async Task HandleLogin(InternalUser user)
    {
        user.LastLogin = DateTimeOffset.UtcNow;
        await _store.UpdateLastLogin(user.Id, DateTime.UtcNow);
    }
}