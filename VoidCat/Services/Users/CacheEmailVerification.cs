﻿using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

/// <inheritdoc />
public class CacheEmailVerification : BaseEmailVerification
{
    private readonly ICache _cache;

    public CacheEmailVerification(ICache cache, ILogger<CacheEmailVerification> logger, VoidSettings settings,
        RazorPartialToStringRenderer renderer) : base(logger, settings, renderer)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    protected override ValueTask SaveToken(EmailVerificationCode code)
    {
        return _cache.Set(MapToken(code.Code), code, code.Expires - DateTime.UtcNow);
    }

    /// <inheritdoc />
    protected override ValueTask<EmailVerificationCode?> GetToken(Guid user, Guid code)
    {
        return _cache.Get<EmailVerificationCode>(MapToken(code));
    }

    /// <inheritdoc />
    protected override ValueTask DeleteToken(Guid user, Guid code)
    {
        return _cache.Delete(MapToken(code));
    }

    private static string MapToken(Guid id) => $"email-code:{id}";
}