using VoidCat.Database;

namespace VoidCat.Services.Abstractions;

public interface IEmailVerification
{
    /// <summary>
    /// Send email verification code
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    ValueTask<EmailVerification> SendNewCode(User user);

    /// <summary>
    /// Perform account verification
    /// </summary>
    /// <param name="user"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    ValueTask<bool> VerifyCode(User user, Guid code);
}