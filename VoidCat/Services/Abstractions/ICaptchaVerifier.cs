namespace VoidCat.Services.Abstractions;

public interface ICaptchaVerifier
{
    ValueTask<bool> Verify(string? token);
}