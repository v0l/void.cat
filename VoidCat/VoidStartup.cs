﻿using System.Data;
using System.Reflection;
using System.Text;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Npgsql;
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

namespace VoidCat;

public static class VoidStartup
{
    public static void AddDatabaseServices(this IServiceCollection services, VoidSettings voidSettings)
    {
        if (voidSettings.HasRedis())
        {
            var cx = ConnectionMultiplexer.Connect(voidSettings.Redis!);
            services.AddSingleton(cx);
            services.AddSingleton(cx.GetDatabase());
        }

        if (voidSettings.HasPostgres())
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
        }
        else
        {
            services.AddMemoryCache();
            services.AddTransient<ICache, InMemoryCache>();
        }
    }

    public static void AddBaseServices(this IServiceCollection services, VoidSettings voidSettings)
    {
        services.AddStorage(voidSettings);
        services.AddMetrics(voidSettings);
        services.AddPaywallServices(voidSettings);
        services.AddUserServices(voidSettings);
        services.AddVirusScanner(voidSettings);
        services.AddCaptcha(voidSettings);
    }

    public static void AddWebServices(this IServiceCollection services, VoidSettings voidSettings)
    {
        services.AddHttpLogging((o) =>
        {
            o.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                              HttpLoggingFields.ResponsePropertiesAndHeaders;
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

        services.AddTransient<RazorPartialToStringRenderer>();
    }

    public static void AddBackgroundServices(this IServiceCollection services, VoidSettings voidSettings)
    {
        services.AddHostedService<DeleteUnverifiedAccounts>();
        services.AddHostedService<DeleteExpiredFiles>();

        if (voidSettings.HasVirusScanner())
        {
            services.AddHostedService<VirusScannerService>();
        }
    }

    public static void AddMigrations(this IServiceCollection services, VoidSettings voidSettings)
    {
        services.AddTransient<IMigration, PopulateMetadataId>();
        services.AddTransient<IMigration, MigrateToPostgres>();
        services.AddTransient<IMigration, FixSize>();

        if (voidSettings.HasRedis())
        {
            services.AddTransient<IMigration, UserLookupKeyHashMigration>();
        }
    }

    public static JsonSerializerSettings ConfigJsonSettings(JsonSerializerSettings s)
    {
        s.NullValueHandling = NullValueHandling.Ignore;
        s.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
        s.MissingMemberHandling = MissingMemberHandling.Ignore;
        return s;
    }
}