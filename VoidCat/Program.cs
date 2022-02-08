using VoidCat.Model;
using VoidCat.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var configuration = builder.Configuration;
var voidSettings = configuration.GetSection("Settings").Get<VoidSettings>();
services.AddSingleton(voidSettings);

var seqSettings = configuration.GetSection("Seq");
builder.Logging.AddSeq(seqSettings);

services.AddRouting();
services.AddControllers().AddNewtonsoftJson();

services.AddMemoryCache();

services.AddScoped<IFileMetadataStore, LocalDiskFileMetadataStore>();
services.AddScoped<IFileStore, LocalDiskFileIngressFactory>();
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
