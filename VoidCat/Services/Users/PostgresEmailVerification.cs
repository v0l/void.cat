using Dapper;
using VoidCat.Model;

namespace VoidCat.Services.Users;

/// <inheritdoc />
public class PostgresEmailVerification : BaseEmailVerification
{
    private readonly PostgresConnectionFactory _connection;

    public PostgresEmailVerification(ILogger<BaseEmailVerification> logger, VoidSettings settings,
        RazorPartialToStringRenderer renderer, PostgresConnectionFactory connection) : base(logger, settings, renderer)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    protected override async ValueTask SaveToken(EmailVerificationCode code)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(
            @"insert into ""EmailVerification""(""User"", ""Code"", ""Expires"") values(:user, :code, :expires)",
            new
            {
                user = code.User,
                code = code.Code,
                expires = code.Expires.ToUniversalTime()
            });
    }

    /// <inheritdoc />
    protected override async ValueTask<EmailVerificationCode?> GetToken(Guid user, Guid code)
    {
        await using var conn = await _connection.Get();
        return await conn.QuerySingleOrDefaultAsync<EmailVerificationCode>(
            @"select * from ""EmailVerification"" where ""User"" = :user and ""Code"" = :code",
            new {user, code});
    }

    /// <inheritdoc />
    protected override async ValueTask DeleteToken(Guid user, Guid code)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(
            @"delete from ""EmailVerification"" where ""User"" = :user and ""Code"" = :code",
            new {user, code});
    }
}