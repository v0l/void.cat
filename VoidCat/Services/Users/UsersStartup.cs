using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

public static class UsersStartup
{
    public static void AddUserServices(this IServiceCollection services, VoidSettings settings)
    {
        services.AddTransient<IUserManager, UserManager>();
        services.AddTransient<IEmailVerification, EmailVerification>();
        if (settings.Postgres != default)
        {
            services.AddTransient<IUserStore, PostgresUserStore>();
        }
        else
        {
            services.AddTransient<IUserStore, UserStore>();
        }
    }
}