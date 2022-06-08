using Dapper;
using Npgsql;
using VoidCat.Model;

namespace VoidCat.Services.Users;

/// <inheritdoc />
public class PostgresEmailVerification : BaseEmailVerification
{
    private readonly NpgsqlConnection _connection;

    public PostgresEmailVerification(ILogger<BaseEmailVerification> logger, VoidSettings settings,
        RazorPartialToStringRenderer renderer, NpgsqlConnection connection) : base(logger, settings, renderer)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    protected override async ValueTask SaveToken(EmailVerificationCode code)
    {
        await _connection.ExecuteAsync(
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
        return await _connection.QuerySingleOrDefaultAsync<EmailVerificationCode>(
            @"select * from ""EmailVerification"" where ""User"" = :user and ""Code"" = :code",
            new {user, code});
    }

    /// <inheritdoc />
    protected override async ValueTask DeleteToken(Guid user, Guid code)
    {
        await _connection.ExecuteAsync(@"delete from ""EmailVerification"" where ""User"" = :user and ""Code"" = :code",
            new {user, code});
    }
}