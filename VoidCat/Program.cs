using Newtonsoft.Json;
using Prometheus;
using VoidCat;
using VoidCat.Model;
using VoidCat.Services.Migrations;

JsonConvert.DefaultSettings = () => VoidStartup.ConfigJsonSettings(new());

RunModes mode = args.Length == 0 ? RunModes.All : 0;

if (args.Contains("--run-migrations"))
{
    mode |= RunModes.Migrations;
}

if (args.Contains("--run-background-jobs"))
{
    mode |= RunModes.BackgroundJobs;
}

Console.WriteLine($"Running with modes: {mode}");

async Task RunMigrations(IServiceProvider services)
{
    using var migrationScope = services.CreateScope();
    var migrations = migrationScope.ServiceProvider.GetServices<IMigration>();
    var logger = migrationScope.ServiceProvider.GetRequiredService<ILogger<IMigration>>();
    foreach (var migration in migrations.OrderBy(a => a.Order))
    {
        logger.LogInformation("Running migration: {Migration}", migration.GetType().Name);
        var res = await migration.Migrate(args);
        logger.LogInformation("== Result: {Result}", res.ToString());
        if (res == IMigration.MigrationResult.ExitCompleted)
        {
            return;
        }
    }
}

if (mode.HasFlag(RunModes.Webserver))
{
    var builder = WebApplication.CreateBuilder(args);
    var services = builder.Services;

    var configuration = builder.Configuration;
    var voidSettings = configuration.GetSection("Settings").Get<VoidSettings>();
    services.AddSingleton(voidSettings);
    services.AddSingleton(voidSettings.Strike ?? new());

    var seqSettings = configuration.GetSection("Seq");
    builder.Logging.AddSeq(seqSettings);

    services.AddBaseServices(voidSettings);
    services.AddDatabaseServices(voidSettings);
    services.AddWebServices(voidSettings);

    if (mode.HasFlag(RunModes.Migrations))
    {
        services.AddMigrations(voidSettings);
    }

    if (mode.HasFlag(RunModes.BackgroundJobs))
    {
        services.AddBackgroundServices(voidSettings);
    }

    var app = builder.Build();

    if (mode.HasFlag(RunModes.Migrations))
    {
        await RunMigrations(app.Services);
    }

#if HostSPA
    app.UseStaticFiles();
#endif

    app.UseHttpLogging();
    app.UseRouting();
    app.UseCors();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseHealthChecks("/healthz");

    app.UseEndpoints(ep =>
    {
        ep.MapControllers();
        ep.MapMetrics();
        ep.MapRazorPages();
#if HostSPA
        ep.MapFallbackToFile("index.html");
#endif
    });

    app.Run();
}
else
{
    // daemon style, dont run web server
    var builder = Host.CreateDefaultBuilder(args);
    builder.ConfigureServices((context, services) =>
    {
        var voidSettings = context.Configuration.GetSection("Settings").Get<VoidSettings>();
        services.AddSingleton(voidSettings);
        services.AddSingleton(voidSettings.Strike ?? new());

        services.AddBaseServices(voidSettings);
        services.AddDatabaseServices(voidSettings);
        if (mode.HasFlag(RunModes.Migrations))
        {
            services.AddMigrations(voidSettings);
        }

        if (mode.HasFlag(RunModes.BackgroundJobs))
        {
            services.AddBackgroundServices(voidSettings);
        }
    });
    builder.ConfigureLogging((context, logging) => { logging.AddSeq(context.Configuration.GetSection("Seq")); });

    var app = builder.Build();
    if (mode.HasFlag(RunModes.Migrations))
    {
        await RunMigrations(app.Services);
    }

    if (mode.HasFlag(RunModes.BackgroundJobs))
    {
        app.Run();
    }
}

[Flags]
internal enum RunModes
{
    Webserver = 1,
    BackgroundJobs = 2,
    Migrations = 4,
    All = 255
}