using nClam;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.VirusScanner;

public static class VirusScannerStartup
{
    public static void AddVirusScanner(this IServiceCollection services, VoidSettings settings)
    {
        services.AddTransient<IVirusScanStore, VirusScanStore>();
        
        var avSettings = settings.VirusScanner;
        if (avSettings != default)
        {
            services.AddHostedService<Background.VirusScannerService>();
            
            // load ClamAV scanner
            if (avSettings.ClamAV != default)
            {
                services.AddTransient<IClamClient>((_) =>
                    new ClamClient(avSettings.ClamAV.Host, avSettings.ClamAV.Port));
                services.AddTransient<IVirusScanner, ClamAvScanner>();
            }
        }
    }
}