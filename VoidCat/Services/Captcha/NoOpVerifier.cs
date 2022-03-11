using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Captcha;

public class NoOpVerifier : ICaptchaVerifier
{
    public ValueTask<bool> Verify(string? token)
    {
        return ValueTask.FromResult(token == null);
    }
}