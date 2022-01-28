using VoidCat.Model;
using VoidCat.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var configuration = builder.Configuration;
var voidSettings = configuration.GetSection("Settings").Get<VoidSettings>();
services.AddSingleton(voidSettings);

// config logging
if (voidSettings?.SeqHost != default)
{
    builder.Logging.AddSeq(voidSettings.SeqHost.ToString(), voidSettings.SeqApiKey);
}

services.AddRouting();
services.AddControllers().AddNewtonsoftJson();

services.AddMemoryCache();

services.AddScoped<IFileStorage, LocalDiskFileIngressFactory>();
services.AddScoped<IStatsCollector, InMemoryStatsCollector>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseEndpoints(ep =>
{
    ep.MapControllers();
    ep.MapFallbackToFile("index.html");
});

app.Run();
