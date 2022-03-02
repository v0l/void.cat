using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IEmailVerification
{
    ValueTask<EmailVerificationCode> SendNewCode(PrivateVoidUser user);

    ValueTask<bool> VerifyCode(PrivateVoidUser user, Guid code);
}