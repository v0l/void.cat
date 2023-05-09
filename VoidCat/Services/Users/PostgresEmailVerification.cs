using Microsoft.EntityFrameworkCore;
using VoidCat.Database;
using VoidCat.Model;

namespace VoidCat.Services.Users;

/// <inheritdoc />
public class PostgresEmailVerification : BaseEmailVerification
{
    private readonly VoidContext _db;

    public PostgresEmailVerification(ILogger<BaseEmailVerification> logger, VoidSettings settings,
        RazorPartialToStringRenderer renderer, VoidContext db) : base(logger, settings, renderer)
    {
        _db = db;
    }

    /// <inheritdoc />
    protected override async ValueTask SaveToken(EmailVerification code)
    {
        _db.EmailVerifications.Add(code);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    protected override async ValueTask<EmailVerification?> GetToken(Guid user, Guid code)
    {
        return await _db.EmailVerifications
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.UserId == user && a.Code == code);
    }

    /// <inheritdoc />
    protected override async ValueTask DeleteToken(Guid user, Guid code)
    {
        await _db.EmailVerifications
            .Where(a => a.UserId == user && a.Code == code)
            .ExecuteDeleteAsync();
    }
}