using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Captcha;

/// <summary>
/// No captcha system is configured
/// </summary>
public class NoOpVerifier : ICaptchaVerifier
{
    /// <inheritdoc />
    public ValueTask<bool> Verify(string? token)
    {
        return ValueTask.FromResult(true);
    }
}