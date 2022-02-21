using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Prometheus;
using StackExchange.Redis;
using VoidCat.Model;
using VoidCat.Services;
using VoidCat.Services.Abstractions;
using VoidCat.Services.InMemory;
using VoidCat.Services.Migrations;
using VoidCat.Services.Paywall;
using VoidCat.Services.Redis;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var configuration = builder.Configuration;
var voidSettings = configuration.GetSection("Settings").Get<VoidSettings>();
services.AddSingleton(voidSettings);
services.AddSingleton(voidSettings.Strike ?? new());

var seqSettings = configuration.GetSection("Seq");
builder.Logging.AddSeq(seqSettings);

var useRedis = !string.IsNullOrEmpty(voidSettings.Redis);
if (useRedis)
{
    var cx = await ConnectionMultiplexer.ConnectAsync(voidSettings.Redis);
    services.AddSingleton(cx);
    services.AddSingleton(cx.GetDatabase());
}

services.AddRouting();
services.AddControllers().AddNewtonsoftJson((opt) =>
{
    opt.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
});
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = voidSettings.JwtSettings.Issuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(voidSettings.JwtSettings.Key))
        };
    });

// void.cat services
//
services.AddVoidMigrations();

// file storage
services.AddTransient<IFileMetadataStore, LocalDiskFileMetadataStore>();
services.AddTransient<IFileStore, LocalDiskFileStore>();

// stats
services.AddTransient<IAggregateStatsCollector, AggregateStatsCollector>();
services.AddTransient<IStatsCollector, PrometheusStatsCollector>();

// paywall
services.AddVoidPaywall();

if (useRedis)
{
    services.AddTransient<RedisStatsController>();
    services.AddTransient<IStatsCollector>(svc => svc.GetRequiredService<RedisStatsController>());
    services.AddTransient<IStatsReporter>(svc => svc.GetRequiredService<RedisStatsController>());
    services.AddTransient<IPaywallStore, RedisPaywallStore>();
}
else
{
    services.AddMemoryCache();
    services.AddTransient<InMemoryStatsController>();
    services.AddTransient<IStatsReporter>(svc => svc.GetRequiredService<InMemoryStatsController>());
    services.AddTransient<IStatsCollector>(svc => svc.GetRequiredService<InMemoryStatsController>());
    services.AddTransient<IPaywallStore, InMemoryPaywallStore>();
}

var app = builder.Build();

// run migrations
var migrations = app.Services.GetServices<IMigration>();
foreach (var migration in migrations)
{
    await migration.Migrate();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(ep =>
{
    ep.MapControllers();
    ep.MapMetrics();
    ep.MapFallbackToFile("index.html");
});

app.Run();