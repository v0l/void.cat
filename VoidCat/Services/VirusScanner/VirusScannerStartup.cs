using nClam;
using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.VirusScanner.VirusTotal;

namespace VoidCat.Services.VirusScanner;

public static class VirusScannerStartup
{
    public static void AddVirusScanner(this IServiceCollection services, VoidSettings settings)
    {
        services.AddTransient<IVirusScanStore, VirusScanStore>();

        var avSettings = settings.VirusScanner;
        if (avSettings != default)
        {
            var loadService = false;

            // load ClamAV scanner
            if (avSettings.ClamAV != default)
            {
                loadService = true;
                services.AddTransient<IClamClient>((_) =>
                    new ClamClient(avSettings.ClamAV.Endpoint.Host, avSettings.ClamAV.Endpoint.Port)
                    {
                        MaxStreamSize = avSettings.ClamAV.MaxStreamSize ?? 26240000
                    });
                services.AddTransient<IVirusScanner, ClamAvScanner>();
            }

            // load VirusTotal
            if (avSettings.VirusTotal != default)
            {
                loadService = true;
                services.AddTransient((svc) =>
                    new VirusTotalClient(svc.GetRequiredService<IHttpClientFactory>(), avSettings.VirusTotal));
                services.AddTransient<IVirusScanner, VirusTotalScanner>();
            }

            if (loadService)
            {
                services.AddHostedService<Background.VirusScannerService>();
            }
        }
    }
}