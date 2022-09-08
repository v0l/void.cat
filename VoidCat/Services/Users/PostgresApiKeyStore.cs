using Dapper;
using VoidCat.Model.User;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

/// <inheritdoc />
public class PostgresApiKeyStore : IApiKeyStore
{
    private readonly PostgresConnectionFactory _factory;

    public PostgresApiKeyStore(PostgresConnectionFactory factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public async ValueTask<ApiKey?> Get(Guid id)
    {
        await using var conn = await _factory.Get();
        return await conn.QuerySingleOrDefaultAsync<ApiKey>(@"select * from ""ApiKey"" where ""Id"" = :id", new {id});
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ApiKey>> Get(Guid[] ids)
    {
        await using var conn = await _factory.Get();
        return (await conn.QueryAsync<ApiKey>(@"select * from ""ApiKey"" where ""Id"" in :ids", new {ids})).ToList();
    }

    /// <inheritdoc />
    public async ValueTask Add(Guid id, ApiKey obj)
    {
        await using var conn = await _factory.Get();
        await conn.ExecuteAsync(@"insert into ""ApiKey""(""Id"", ""UserId"", ""Token"", ""Expiry"") 
values(:id, :userId, :token, :expiry)", new
        {
            id = obj.Id,
            userId = obj.UserId,
            token = obj.Token,
            expiry = obj.Expiry.ToUniversalTime()
        });
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await using var conn = await _factory.Get();
        await conn.ExecuteAsync(@"delete from ""ApiKey"" where ""Id"" = :id", new {id});
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ApiKey>> ListKeys(Guid id)
    {
        await using var conn = await _factory.Get();
        return (await conn.QueryAsync<ApiKey>(@"select ""Id"", ""UserId"", ""Expiry"", ""Created"" from ""ApiKey"" where ""UserId"" = :id", new {id}))
            .ToList();
    }
}
