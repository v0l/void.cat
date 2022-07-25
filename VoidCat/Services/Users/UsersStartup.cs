using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

public static class UsersStartup
{
    public static void AddUserServices(this IServiceCollection services, VoidSettings settings)
    {
        services.AddTransient<IUserManager, UserManager>();

        if (settings.HasPostgres())
        {
            services.AddTransient<IUserStore, PostgresUserStore>();
            services.AddTransient<IEmailVerification, PostgresEmailVerification>();
            services.AddTransient<IApiKeyStore, PostgresApiKeyStore>();
        }
        else
        {
            services.AddTransient<IUserStore, CacheUserStore>();
            services.AddTransient<IEmailVerification, CacheEmailVerification>();
            services.AddTransient<IApiKeyStore, CacheApiKeyStore>();
        }
    }
}