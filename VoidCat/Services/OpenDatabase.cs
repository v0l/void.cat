using Npgsql;

public class OpenDatabase : IMiddleware
{
    private readonly NpgsqlConnection _connection;

    public OpenDatabase(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await _connection.OpenAsync();
        try
        {
            await next(context);
        }
        finally
        {
            await _connection.CloseAsync();
        }
    }
}