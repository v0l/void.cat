using Dapper;
using VoidCat.Model.User;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users.Auth;

/// <inheritdoc />
public class PostgresUserAuthTokenStore : IUserAuthTokenStore
{
    private readonly PostgresConnectionFactory _connection;

    public PostgresUserAuthTokenStore(PostgresConnectionFactory connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public async ValueTask<UserAuthToken?> Get(Guid id)
    {
        await using var conn = await _connection.Get();
        return await conn.QuerySingleOrDefaultAsync<UserAuthToken>(
            @"select * from ""UsersAuthToken"" where ""User"" = :id", new {id});
    }

    /// <inheritdoc />
    public async ValueTask Add(Guid id, UserAuthToken obj)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(
            @"insert into ""UsersAuthToken""(""Id"", ""User"", ""Provider"", ""AccessToken"", ""TokenType"", ""Expires"", ""RefreshToken"", ""Scope"")
values(:id, :user, :provider, :accessToken, :tokenType, :expires, :refreshToken, :scope)
on conflict(""Id"") do update set
""AccessToken"" = :accessToken,
""TokenType"" = :tokenType,
""Expires"" = :expires,
""RefreshToken"" = :refreshToken,
""Scope"" = :scope", new
            {
                id = obj.Id,
                user = obj.User,
                provider = obj.Provider,
                accessToken = obj.AccessToken,
                tokenType = obj.TokenType,
                expires = obj.Expires.ToUniversalTime(),
                refreshToken = obj.RefreshToken,
                scope = obj.Scope
            });
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(@"delete from ""UsersAuthToken"" where ""Id"" = :id", new {id});
    }
}