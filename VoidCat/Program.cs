using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
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

services.AddMemoryCache();

services.AddScoped<IFileMetadataStore, LocalDiskFileMetadataStore>();
services.AddScoped<IFileStore, LocalDiskFileStore>();
services.AddScoped<IStatsCollector, PrometheusStatsCollector>();

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
