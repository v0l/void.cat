using Dapper;
using Npgsql;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

public class PostgreFileMetadataStore : IFileMetadataStore
{
    private readonly NpgsqlConnection _connection;

    public PostgreFileMetadataStore(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public ValueTask<VoidFileMeta?> Get(Guid id)
    {
        return Get<VoidFileMeta>(id);
    }

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

    public async ValueTask Delete(Guid id)
    {
        await _connection.ExecuteAsync("delete from \"Files\" where \"Id\" = :id", new {id});
    }

    public async ValueTask<TMeta?> Get<TMeta>(Guid id) where TMeta : VoidFileMeta
    {
        return await _connection.QuerySingleOrDefaultAsync<TMeta?>(@"select * from ""Files"" where ""Id"" = :id",
            new {id});
    }

    public async ValueTask<IReadOnlyList<TMeta>> Get<TMeta>(Guid[] ids) where TMeta : VoidFileMeta
    {
        var ret = await _connection.QueryAsync<TMeta>("select * from \"Files\" where \"Id\" in :ids", new {ids});
        return ret.ToList();
    }

    public async ValueTask Update<TMeta>(Guid id, TMeta meta) where TMeta : VoidFileMeta
    {
        var oldMeta = await Get<SecretVoidFileMeta>(id);
        if (oldMeta == default) return;

        oldMeta.Description = meta.Description ?? oldMeta.Description;
        oldMeta.Name = meta.Name ?? oldMeta.Name;
        oldMeta.MimeType = meta.MimeType ?? oldMeta.MimeType;

        await Set(id, oldMeta);
    }

    public async ValueTask<IFileMetadataStore.StoreStats> Stats()
    {
        var v = await _connection.QuerySingleAsync<(long Files, long Size)>(
            @"select count(1) ""Files"", cast(sum(""Size"") as bigint) ""Size"" from ""Files""");
        return new(v.Files, (ulong) v.Size);
    }
}