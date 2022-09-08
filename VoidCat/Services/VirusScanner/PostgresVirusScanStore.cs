using Dapper;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.VirusScanner;

/// <inheritdoc />
public class PostgresVirusScanStore : IVirusScanStore
{
    private readonly PostgresConnectionFactory _connection;

    public PostgresVirusScanStore(PostgresConnectionFactory connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public async ValueTask<VirusScanResult?> Get(Guid id)
    {
        await using var conn = await _connection.Get();
        return await conn.QuerySingleOrDefaultAsync<VirusScanResult>(
            @"select * from ""VirusScanResult"" where ""Id"" = :id", new {id});
    }

    /// <inheritdoc />
    public async ValueTask<VirusScanResult?> GetByFile(Guid id)
    {
        await using var conn = await _connection.Get();
        return await conn.QuerySingleOrDefaultAsync<VirusScanResult>(
            @"select * from ""VirusScanResult"" where ""File"" = :file", new {file = id});
    }

    /// <inheritdoc />
    public async ValueTask Add(Guid id, VirusScanResult obj)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(
            @"insert into ""VirusScanResult""(""Id"", ""File"", ""Scanner"", ""Score"", ""Names"") values(:id, :file, :scanner, :score, :names)",
            new
            {
                id,
                file = obj.File,
                scanner = obj.Scanner,
                score = obj.Score,
                names = obj.Names
            });
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(@"delete from ""VirusScanResult"" where ""Id"" = :id", new {id});
    }
}