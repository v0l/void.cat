using Dapper;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

/// <inheritdoc />
public class PostgresFileMetadataStore : IFileMetadataStore
{
    private readonly PostgresConnectionFactory _connection;

    public PostgresFileMetadataStore(PostgresConnectionFactory connection)
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
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(
            @"insert into 
""Files""(""Id"", ""Name"", ""Size"", ""Uploaded"", ""Description"", ""MimeType"", ""Digest"", ""EditSecret"", ""Expires"")
values(:id, :name, :size, :uploaded, :description, :mimeType, :digest, :editSecret, :expires)
on conflict (""Id"") do update set ""Name"" = :name, ""Size"" = :size, ""Description"" = :description, ""MimeType"" = :mimeType, ""Expires"" = :expires",
            new
            {
                id,
                name = obj.Name,
                size = (long) obj.Size,
                uploaded = obj.Uploaded.ToUniversalTime(),
                description = obj.Description,
                mimeType = obj.MimeType,
                digest = obj.Digest,
                editSecret = obj.EditSecret,
                expires = obj.Expires
            });
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync("delete from \"Files\" where \"Id\" = :id", new {id});
    }

    /// <inheritdoc />
    public async ValueTask<TMeta?> Get<TMeta>(Guid id) where TMeta : VoidFileMeta
    {
        await using var conn = await _connection.Get();
        return await conn.QuerySingleOrDefaultAsync<TMeta?>(@"select * from ""Files"" where ""Id"" = :id",
            new {id});
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<TMeta>> Get<TMeta>(Guid[] ids) where TMeta : VoidFileMeta
    {
        await using var conn = await _connection.Get();
        var ret = await conn.QueryAsync<TMeta>("select * from \"Files\" where \"Id\" in :ids", new {ids});
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
        oldMeta.Expires = meta.Expires ?? oldMeta.Expires;

        await Set(id, oldMeta);
    }

    /// <inheritdoc />
    public async ValueTask<PagedResult<TMeta>> ListFiles<TMeta>(PagedRequest request) where TMeta : VoidFileMeta
    {
        await using var conn = await _connection.Get();
        var count = await conn.ExecuteScalarAsync<int>(@"select count(*) from ""Files""");

        async IAsyncEnumerable<TMeta> Enumerate()
        {
            var orderBy = request.SortBy switch
            {
                PagedSortBy.Date => "Uploaded",
                PagedSortBy.Name => "Name",
                PagedSortBy.Size => "Size",
                _ => "Id"
            };
            await using var iconn = await _connection.Get();
            var orderDirection = request.SortOrder == PageSortOrder.Asc ? "asc" : "desc";
            var results = await iconn.QueryAsync<TMeta>(
                $"select * from \"Files\" order by \"{orderBy}\" {orderDirection} offset @offset limit @limit",
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
        await using var conn = await _connection.Get();
        var v = await conn.QuerySingleAsync<(long Files, long Size)>(
            @"select count(1) ""Files"", cast(sum(""Size"") as bigint) ""Size"" from ""Files""");
        return new(v.Files, (ulong) v.Size);
    }
}