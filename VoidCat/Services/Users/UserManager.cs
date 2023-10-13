using VoidCat.Database;
using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Users.Auth;

namespace VoidCat.Services.Users;

public class UserManager
{
    private readonly IUserStore _store;
    private readonly IEmailVerification _emailVerification;
    private readonly OAuthFactory _oAuthFactory;
    private readonly NostrProfileService _nostrProfile;
    private static bool _checkFirstRegister;

    public UserManager(IUserStore store, IEmailVerification emailVerification, OAuthFactory oAuthFactory, NostrProfileService nostrProfile)
    {
        _store = store;
        _emailVerification = emailVerification;
        _oAuthFactory = oAuthFactory;
        _nostrProfile = nostrProfile;
    }

    /// <summary>
    /// Login an existing user with email/password
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async ValueTask<User> Login(string email, string password)
    {
        var userId = await _store.LookupUser(email);
        if (!userId.HasValue) throw new InvalidOperationException("User does not exist");

        var user = await _store.Get(userId.Value);
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
    public async ValueTask<User> Register(string email, string password)
    {
        var existingUser = await _store.LookupUser(email);
        if (existingUser != Guid.Empty && existingUser != null)
            throw new InvalidOperationException("User already exists");

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Password = password.HashPassword(),
            Created = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow
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
    public async ValueTask<User> LoginOrRegister(string code, string provider)
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
            var existingUser = await _store.Get(uid.Value);
            if (existingUser?.AuthType == UserAuthType.OAuth2)
            {
                return existingUser;
            }

            throw new InvalidOperationException("Auth failure, user type does not match!");
        }

        await SetupNewUser(user);
        return user;
    }

    /// <summary>
    /// Login or Register with nostr pubkey
    /// </summary>
    /// <param name="pubkey">Hex public key</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async ValueTask<User> LoginOrRegister(string pubkey)
    {
        var uid = await _store.LookupUser(pubkey);
        if (uid.HasValue)
        {
            var existingUser = await _store.Get(uid.Value);
            if (existingUser?.AuthType == UserAuthType.Nostr)
            {
                return existingUser;
            }

            throw new InvalidOperationException("Auth failure, user type does not match!");
        }

        var profile = await _nostrProfile.FetchProfile(pubkey);
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            AuthType = UserAuthType.Nostr,
            Created = DateTime.UtcNow,
            Avatar = profile?.Picture,
            DisplayName = profile?.Name ?? "Nostrich",
            Email = pubkey,
            Flags = UserFlags.EmailVerified // always mark as email verififed
        };

        await SetupNewUser(newUser);
        return newUser;
    }

    private async Task SetupNewUser(User newUser)
    {
        // automatically set first user to admin
        if (!_checkFirstRegister)
        {
            _checkFirstRegister = true;
            var users = await _store.ListUsers(new(0, 1));
            if (users.TotalResults == 0)
            {
                newUser.Flags |= UserFlags.EmailVerified; // force email as verified for admin user
                newUser.Roles.Add(new()
                {
                    UserId = newUser.Id,
                    Role = Roles.Admin
                });
            }
        }

        await _store.Add(newUser);
        if (!newUser.Flags.HasFlag(UserFlags.EmailVerified))
        {
            await _emailVerification.SendNewCode(newUser);
        }
    }

    private async Task HandleLogin(User user)
    {
        user.LastLogin = DateTime.UtcNow;
        await _store.UpdateLastLogin(user.Id, DateTime.UtcNow);
    }
}
