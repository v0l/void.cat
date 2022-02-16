using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using StackExchange.Redis;
using VoidCat.Model;
using VoidCat.Services;
using VoidCat.Services.Abstractions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var configuration = builder.Configuration;
var voidSettings = configuration.GetSection("Settings").Get<VoidSettings>();
services.AddSingleton(voidSettings);

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
services.AddControllers().AddNewtonsoftJson();
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

services.AddScoped<IFileMetadataStore, LocalDiskFileMetadataStore>();
services.AddScoped<IFileStore, LocalDiskFileStore>();
services.AddScoped<IAggregateStatsCollector, AggregateStatsCollector>();
services.AddScoped<IStatsCollector, PrometheusStatsCollector>();
if (useRedis)
{
    services.AddScoped<RedisStatsController>();
    services.AddScoped<IStatsCollector>(svc => svc.GetRequiredService<RedisStatsController>());
    services.AddScoped<IStatsReporter>(svc => svc.GetRequiredService<RedisStatsController>());
}
else
{
    services.AddMemoryCache();
    services.AddScoped<InMemoryStatsController>();
    services.AddScoped<IStatsReporter>(svc => svc.GetRequiredService<InMemoryStatsController>());
    services.AddScoped<IStatsCollector>(svc => svc.GetRequiredService<InMemoryStatsController>());
}

var app = builder.Build();

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
