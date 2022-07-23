using System.Data;
using System.Reflection;
using System.Text;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Npgsql;
using Prometheus;
using StackExchange.Redis;
using VoidCat.Model;
using VoidCat.Services;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Background;
using VoidCat.Services.Captcha;
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

if (voidSettings.HasRedis())
{
    var cx = await ConnectionMultiplexer.ConnectAsync(voidSettings.Redis);
    services.AddSingleton(cx);
    services.AddSingleton(cx.GetDatabase());
}

services.AddHttpLogging((o) =>
{
    o.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.ResponsePropertiesAndHeaders;
    o.RequestBodyLogLimit = 4096;
    o.ResponseBodyLogLimit = 4096;

    o.MediaTypeOptions.Clear();
    o.MediaTypeOptions.AddText("application/json");

    foreach (var h in voidSettings.RequestHeadersLog ?? Enumerable.Empty<string>())
    {
        o.RequestHeaders.Add(h);
    }
});
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
services.AddHealthChecks();

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
services.AddTransient<IMigration, PopulateMetadataId>();
services.AddTransient<IMigration, MigrateToPostgres>();
services.AddTransient<IMigration, FixSize>();

// file storage
services.AddStorage(voidSettings);

// stats
services.AddMetrics(voidSettings);

// paywall
services.AddPaywallServices(voidSettings);

// users
services.AddUserServices(voidSettings);

// background services
services.AddHostedService<DeleteUnverifiedAccounts>();

// virus scanner
services.AddVirusScanner(voidSettings);

// captcha
services.AddCaptcha(voidSettings);

// postgres
if (!string.IsNullOrEmpty(voidSettings.Postgres))
{
    services.AddSingleton<PostgresConnectionFactory>();
    services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(voidSettings.Postgres));

    // fluent migrations
    services.AddTransient<IMigration, FluentMigrationRunner>();
    services.AddFluentMigratorCore()
        .ConfigureRunner(r =>
            r.AddPostgres()
                .WithGlobalConnectionString(voidSettings.Postgres)
                .ScanIn(typeof(Program).Assembly).For.Migrations());
}

if (voidSettings.HasRedis())
{
    services.AddTransient<ICache, RedisCache>();

    // redis specific migrations
    services.AddTransient<IMigration, UserLookupKeyHashMigration>();
}
else
{
    services.AddMemoryCache();
    services.AddTransient<ICache, InMemoryCache>();
}

var app = builder.Build();

// run migrations
using (var migrationScope = app.Services.CreateScope())
{
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