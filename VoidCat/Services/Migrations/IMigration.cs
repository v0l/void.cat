﻿namespace VoidCat.Services.Migrations;

public interface IMigration
{
    ValueTask Migrate();
}

public static class Migrations
{
    public static IServiceCollection AddVoidMigrations(this IServiceCollection svc)
    {
        svc.AddTransient<IMigration, MigrateMetadata_20220217>();
        svc.AddTransient<IMigration, FixMigration_20220218>();
        return svc;
    }
}