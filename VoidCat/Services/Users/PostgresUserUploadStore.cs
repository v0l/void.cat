using Dapper;
using Npgsql;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

public class PostgresUserUploadStore : IUserUploadsStore
{
    private readonly NpgsqlConnection _connection;
    private readonly IFileInfoManager _fileInfoManager;

    public PostgresUserUploadStore(NpgsqlConnection connection, IFileInfoManager fileInfoManager)
    {
        _connection = connection;
        _fileInfoManager = fileInfoManager;
    }

    public async ValueTask<PagedResult<PublicVoidFile>> ListFiles(Guid user, PagedRequest request)
    {
        var query = @"select {0} 
from ""UserFiles"" uf, ""Files"" f
where uf.""User"" = :user
and uf.""File"" = f.""Id""";
        var queryOrder = @"order by f.""{1}"" {2} limit :limit offset :offset";

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
        var count = await _connection.ExecuteScalarAsync<int>(string.Format(query, "count(*)"), new {user});
        var files = await _connection.QueryAsync<Guid>(
            string.Format(query + queryOrder, "uf.\"File\"", orderBy, sortOrder),
            new {user, offset = request.Page * request.PageSize, limit = request.PageSize});

        async IAsyncEnumerable<PublicVoidFile> EnumerateFiles()
        {
            foreach (var file in files ?? Enumerable.Empty<Guid>())
            {
                var v = await _fileInfoManager.Get(file);
                if (v != default)
                {
                    yield return v;
                }
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

    public async ValueTask AddFile(Guid user, PrivateVoidFile voidFile)
    {
        await _connection.ExecuteAsync(@"insert into ""UserFiles""(""File"", ""User"") values(:file, :user)", new
        {
            file = voidFile.Id,
            user
        });
    }
}