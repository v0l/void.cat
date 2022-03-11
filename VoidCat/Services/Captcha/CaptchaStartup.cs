using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Captcha;

public static class CaptchaStartup
{
    public static void AddCaptcha(this IServiceCollection services, VoidSettings settings)
    {
        if (settings.CaptchaSettings != default)
        {
            services.AddTransient<ICaptchaVerifier, hCaptchaVerifier>();
        }
        else
        {
            services.AddTransient<ICaptchaVerifier, NoOpVerifier>();
        }
    }
}