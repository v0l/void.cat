using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Users.Auth;

namespace VoidCat.Services.Users;

public static class UsersStartup
{
    public static void AddUserServices(this IServiceCollection services, VoidSettings settings)
    {
        services.AddTransient<UserManager>();
        services.AddTransient<OAuthFactory>();
        
        if (settings.HasDiscord())
        {
            services.AddTransient<IOAuthProvider, DiscordOAuthProvider>();
        }

        if (settings.HasPostgres())
        {
            services.AddTransient<IUserStore, PostgresUserStore>();
            services.AddTransient<IEmailVerification, PostgresEmailVerification>();
            services.AddTransient<IApiKeyStore, PostgresApiKeyStore>();
            services.AddTransient<IUserAuthTokenStore, PostgresUserAuthTokenStore>();
        }
        else
        {
            services.AddTransient<IUserStore, CacheUserStore>();
            services.AddTransient<IEmailVerification, CacheEmailVerification>();
            services.AddTransient<IApiKeyStore, CacheApiKeyStore>();
            services.AddTransient<IUserAuthTokenStore, CacheUserAuthTokenStore>();
        }
    }
}