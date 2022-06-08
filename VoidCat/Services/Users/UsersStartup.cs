using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

public static class UsersStartup
{
    public static void AddUserServices(this IServiceCollection services, VoidSettings settings)
    {
        services.AddTransient<IUserManager, UserManager>();

        if (settings.Postgres != default)
        {
            services.AddTransient<IUserStore, PostgresUserStore>();
            services.AddTransient<IEmailVerification, PostgresEmailVerification>();
        }
        else
        {
            services.AddTransient<IUserStore, CacheUserStore>();
            services.AddTransient<IEmailVerification, CacheEmailVerification>();
        }
    }
}