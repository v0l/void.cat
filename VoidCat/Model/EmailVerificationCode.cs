namespace VoidCat.Model;

/// <summary>
/// Email verification token
/// </summary>
/// <param name="Id"></param>
/// <param name="User"></param>
/// <param name="Expires"></param>
public sealed record EmailVerificationCode(Guid User, Guid Code, DateTime Expires);