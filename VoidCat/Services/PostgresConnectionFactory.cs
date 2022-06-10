using System.Data;
using Npgsql;
using VoidCat.Model;

namespace VoidCat.Services;

public sealed class PostgresConnectionFactory
{
    private readonly VoidSettings _settings;

    public PostgresConnectionFactory(VoidSettings settings)
    {
        _settings = settings;
    }

    public async Task<NpgsqlConnection> Get()
    {
        var conn = new NpgsqlConnection(_settings.Postgres);
        if (!conn.State.HasFlag(ConnectionState.Open))
        {
            await conn.OpenAsync();
        }

        return conn;
    }
}