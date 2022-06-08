using Dapper;
using Npgsql;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

/// <inheritdoc />
public class PostgresFileMetadataStore : IFileMetadataStore
{
    private readonly NpgsqlConnection _connection;

    public PostgresFileMetadataStore(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public ValueTask<VoidFileMeta?> Get(Guid id)
    {
        return Get<VoidFileMeta>(id);
    }

    /// <inheritdoc />
    public ValueTask<SecretVoidFileMeta?> GetPrivate(Guid id)
    {
        return Get<SecretVoidFileMeta>(id);
    }

    /// <inheritdoc />
    public async ValueTask Set(Guid id, SecretVoidFileMeta obj)
    {
        await _connection.ExecuteAsync(
            @"insert into 
""Files""(""Id"", ""Name"", ""Size"", ""Uploaded"", ""Description"", ""MimeType"", ""Digest"", ""EditSecret"")
values(:id, :name, :size, :uploaded, :description, :mimeType, :digest, :editSecret)
on conflict (""Id"") do update set ""Name"" = :name, ""Description"" = :description, ""MimeType"" = :mimeType", new
            {
                id,
                name = obj.Name,
                size = (long) obj.Size,
                uploaded = obj.Uploaded.ToUniversalTime(),
                description = obj.Description,
                mimeType = obj.MimeType,
                digest = obj.Digest,
                editSecret = obj.EditSecret
            });
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await _connection.ExecuteAsync("delete from \"Files\" where \"Id\" = :id", new {id});
    }

    /// <inheritdoc />
    public async ValueTask<TMeta?> Get<TMeta>(Guid id) where TMeta : VoidFileMeta
    {
        return await _connection.QuerySingleOrDefaultAsync<TMeta?>(@"select * from ""Files"" where ""Id"" = :id",
            new {id});
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<TMeta>> Get<TMeta>(Guid[] ids) where TMeta : VoidFileMeta
    {
        var ret = await _connection.QueryAsync<TMeta>("select * from \"Files\" where \"Id\" in :ids", new {ids});
        return ret.ToList();
    }

    /// <inheritdoc />
    public async ValueTask Update<TMeta>(Guid id, TMeta meta) where TMeta : VoidFileMeta
    {
        var oldMeta = await Get<SecretVoidFileMeta>(id);
        if (oldMeta == default) return;

        oldMeta.Description = meta.Description ?? oldMeta.Description;
        oldMeta.Name = meta.Name ?? oldMeta.Name;
        oldMeta.MimeType = meta.MimeType ?? oldMeta.MimeType;

        await Set(id, oldMeta);
    }

    /// <inheritdoc />
    public async ValueTask<PagedResult<TMeta>> ListFiles<TMeta>(PagedRequest request) where TMeta : VoidFileMeta
    {
        var qInner = @"select {0} from ""Files"" order by ""{1}"" {2}";
        var orderBy = request.SortBy switch
        {
            PagedSortBy.Date => "Uploaded",
            PagedSortBy.Name => "Name",
            PagedSortBy.Size => "Size",
            _ => "Id"
        };
        var orderDirection = request.SortOrder == PageSortOrder.Asc ? "asc" : "desc";
        var count = await _connection.ExecuteScalarAsync<int>(string.Format(qInner, "count(*)", orderBy,
            orderDirection));

        async IAsyncEnumerable<TMeta> Enumerate()
        {
            var results = await _connection.QueryAsync<TMeta>(
                $"{string.Format(qInner, "*", orderBy, orderDirection)} offset @offset limit @limit",
                new {offset = request.PageSize * request.Page, limit = request.PageSize});

            foreach (var meta in results)
            {
                yield return meta;
            }
        }

        return new()
        {
            TotalResults = count,
            PageSize = request.PageSize,
            Page = request.Page,
            Results = Enumerate()
        };
    }

    /// <inheritdoc />
    public async ValueTask<IFileMetadataStore.StoreStats> Stats()
    {
        var v = await _connection.QuerySingleAsync<(long Files, long Size)>(
            @"select count(1) ""Files"", cast(sum(""Size"") as bigint) ""Size"" from ""Files""");
        return new(v.Files, (ulong) v.Size);
    }
}