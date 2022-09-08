using VoidCat.Model;
using VoidCat.Model.User;

namespace VoidCat.Services.Abstractions;

public interface IEmailVerification
{
    ValueTask<EmailVerificationCode> SendNewCode(PrivateUser user);

    ValueTask<bool> VerifyCode(PrivateUser user, Guid code);
}