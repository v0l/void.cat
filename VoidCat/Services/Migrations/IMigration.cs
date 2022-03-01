namespace VoidCat.Services.Migrations;

public interface IMigration
{
    ValueTask Migrate();
}

public static class Migrations
{
    public static void AddVoidMigrations(this IServiceCollection svc)
    {
    }
}