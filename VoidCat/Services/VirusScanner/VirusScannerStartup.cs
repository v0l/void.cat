using nClam;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.VirusScanner;

public static class VirusScannerStartup
{
    public static void AddVirusScanner(this IServiceCollection services, VoidSettings settings)
    {
        if (settings.Postgres != default)
        {
            services.AddTransient<IVirusScanStore, PostgresVirusScanStore>();
        }
        else
        {
            services.AddTransient<IVirusScanStore, CacheVirusScanStore>();
        }

        var avSettings = settings.VirusScanner;
        if (avSettings != default)
        {
            // load ClamAV scanner
            if (avSettings.ClamAV != default)
            {
                services.AddTransient<IClamClient>((_) =>
                    new ClamClient(avSettings.ClamAV.Endpoint.Host, avSettings.ClamAV.Endpoint.Port)
                    {
                        MaxStreamSize = avSettings.ClamAV.MaxStreamSize ?? 26240000
                    });
                services.AddTransient<IVirusScanner, ClamAvScanner>();
            }
        }
    }
}