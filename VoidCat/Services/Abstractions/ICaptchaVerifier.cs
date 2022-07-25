namespace VoidCat.Services.Abstractions;

public interface ICaptchaVerifier
{
    /// <summary>
    /// Verify captcha token is valid
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    ValueTask<bool> Verify(string? token);
}