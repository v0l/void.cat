using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Prometheus;
using StackExchange.Redis;
using VoidCat.Model;
using VoidCat.Services;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Background;
using VoidCat.Services.Files;
using VoidCat.Services.InMemory;
using VoidCat.Services.Migrations;
using VoidCat.Services.Paywall;
using VoidCat.Services.Redis;
using VoidCat.Services.Stats;
using VoidCat.Services.Users;
using VoidCat.Services.VirusScanner;

// setup JsonConvert default settings
JsonSerializerSettings ConfigJsonSettings(JsonSerializerSettings s)
{
    s.NullValueHandling = NullValueHandling.Ignore;
    s.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
    s.MissingMemberHandling = MissingMemberHandling.Ignore;
    return s;
}

JsonConvert.DefaultSettings = () => ConfigJsonSettings(new());

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

services.AddHttpClient();
services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please insert JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
    var path = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    c.IncludeXmlComments(path);
});
services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p =>
    {
        p.AllowAnyMethod()
            .AllowAnyHeader()
            .WithOrigins(voidSettings.CorsOrigins.Select(a => a.OriginalString).ToArray());
    });
});
services.AddRazorPages();
services.AddRouting();
services.AddControllers()
    .AddNewtonsoftJson((opt) => { ConfigJsonSettings(opt.SerializerSettings); });

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

services.AddAuthorization((opt) =>
{
    opt.AddPolicy(Policies.RequireAdmin, (auth) => { auth.RequireRole(Roles.Admin); });
});

// void.cat services
//
services.AddTransient<RazorPartialToStringRenderer>();
services.AddVoidMigrations();

// file storage
services.AddStorage(voidSettings);

// stats
services.AddTransient<IAggregateStatsCollector, AggregateStatsCollector>();
services.AddTransient<IStatsCollector, PrometheusStatsCollector>();

// paywall
services.AddVoidPaywall();

// users
services.AddTransient<IUserStore, UserStore>();
services.AddTransient<IUserManager, UserManager>();
services.AddTransient<IEmailVerification, EmailVerification>();

// background services
services.AddHostedService<DeleteUnverifiedAccounts>();

// virus scanner
services.AddVirusScanner(voidSettings);

if (useRedis)
{
    services.AddTransient<ICache, RedisCache>();
    services.AddTransient<RedisStatsController>();
    services.AddTransient<IStatsCollector>(svc => svc.GetRequiredService<RedisStatsController>());
    services.AddTransient<IStatsReporter>(svc => svc.GetRequiredService<RedisStatsController>());
}
else
{
    services.AddMemoryCache();
    services.AddTransient<ICache, InMemoryCache>();
    services.AddTransient<InMemoryStatsController>();
    services.AddTransient<IStatsReporter>(svc => svc.GetRequiredService<InMemoryStatsController>());
    services.AddTransient<IStatsCollector>(svc => svc.GetRequiredService<InMemoryStatsController>());
}

var app = builder.Build();

// run migrations
var migrations = app.Services.GetServices<IMigration>();
foreach (var migration in migrations)
{
    await migration.Migrate();
}

#if HostSPA
app.UseStaticFiles();
#endif

app.UseRouting();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

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