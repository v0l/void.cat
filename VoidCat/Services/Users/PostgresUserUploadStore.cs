using Dapper;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

/// <inheritdoc />
public class PostgresUserUploadStore : IUserUploadsStore
{
    private readonly PostgresConnectionFactory _connection;

    public PostgresUserUploadStore(PostgresConnectionFactory connection)
    {
        _connection = connection;
    }

    public async ValueTask<PagedResult<Guid>> ListFiles(Guid user, PagedRequest request)
    {
        var query = @"select {0} 
from ""UserFiles"" uf, ""Files"" f
where uf.""User"" = :user
and uf.""File"" = f.""Id""";
        var queryOrder = @"order by f.""{1}"" {2} limit :limit offset :offset";

        await using var conn = await _connection.Get();
        var count = await conn.ExecuteScalarAsync<int>(string.Format(query, "count(*)"), new {user});

        async IAsyncEnumerable<Guid> EnumerateFiles()
        {
            var orderBy = request.SortBy switch
            {
                PagedSortBy.Name => "Name",
                PagedSortBy.Date => "Uploaded",
                PagedSortBy.Size => "Size",
                _ => "Id"
            };
            var sortOrder = request.SortOrder switch
            {
                PageSortOrder.Dsc => "desc",
                _ => "asc"
            };
            await using var connInner = await _connection.Get();
            var files = await connInner.ExecuteReaderAsync(
                string.Format(query + queryOrder, "uf.\"File\"", orderBy, sortOrder),
                new {user, offset = request.Page * request.PageSize, limit = request.PageSize});
            var rowParser = files.GetRowParser<Guid>();
            while (await files.ReadAsync())
            {
                yield return rowParser(files);
            }
        }

        return new()
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalResults = count,
            Results = EnumerateFiles()
        };
    }

    /// <inheritdoc />
    public async ValueTask AddFile(Guid user, PrivateVoidFile voidFile)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(@"insert into ""UserFiles""(""File"", ""User"") values(:file, :user)", new
        {
            file = voidFile.Id,
            user
        });
    }

    /// <inheritdoc />
    public async ValueTask<Guid?> Uploader(Guid file)
    {
        await using var conn = await _connection.Get();
        return await conn.ExecuteScalarAsync<Guid?>(
            @"select ""User"" from ""UserFiles"" where ""File"" = :file", new {file});
    }
}