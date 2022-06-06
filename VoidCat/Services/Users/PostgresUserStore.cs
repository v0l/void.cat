using Dapper;
using Npgsql;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

public class PostgresUserStore : IUserStore
{
    private readonly NpgsqlConnection _connection;

    public PostgresUserStore(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public ValueTask<VoidUser?> Get(Guid id)
    {
        return Get<VoidUser>(id);
    }

    public async ValueTask Set(Guid id, InternalVoidUser obj)
    {
        await _connection.ExecuteAsync(
            @"insert into 
""Users""(""Id"", ""Email"", ""Password"", ""LastLogin"", ""DisplayName"", ""Avatar"", ""Flags"") 
values(:id, :email, :password, :lastLogin, :displayName, :avatar, :flags)
on conflict (""Id"") do update set ""LastLogin"" = :lastLogin, ""DisplayName"" = :displayName, ""Avatar"" = :avatar, ""Flags"" = :flags",
            new
            {
                Id = id,
                email = obj.Email,
                password = obj.PasswordHash,
                displayName = obj.DisplayName,
                lastLogin = obj.LastLogin,
                avatar = obj.Avatar,
                flags = (int) obj.Flags
            });
    }

    public async ValueTask Delete(Guid id)
    {
        await _connection.ExecuteAsync(@"delete from ""Users"" where ""Id"" = :id", new {id});
    }

    public async ValueTask<T?> Get<T>(Guid id) where T : VoidUser
    {
        return await _connection.QuerySingleOrDefaultAsync<T?>(@"select * from ""Users"" where ""Id"" = :id", new {id});
    }

    public async ValueTask<Guid?> LookupUser(string email)
    {
        return await _connection.QuerySingleOrDefaultAsync<Guid?>(
            @"select ""Id"" from ""Users"" where ""Email"" = :email",
            new {email});
    }

    public async ValueTask<PagedResult<PrivateVoidUser>> ListUsers(PagedRequest request)
    {
        var orderBy = request.SortBy switch
        {
            PagedSortBy.Date => "Created",
            PagedSortBy.Name => "DisplayName",
            _ => "Id"
        };
        var sortBy = request.SortOrder switch
        {
            PageSortOrder.Dsc => "desc",
            _ => "asc"
        };
        var totalUsers = await _connection.ExecuteScalarAsync<int>(@"select count(*) from ""Users""");
        var users = await _connection.QueryAsync<PrivateVoidUser>(
            $@"select * from ""Users"" order by ""{orderBy}"" {sortBy} offset :offset limit :limit",
            new
            {
                offset = request.PageSize * request.Page,
                limit = request.PageSize
            });

        async IAsyncEnumerable<PrivateVoidUser> Enumerate()
        {
            foreach (var u in users ?? Enumerable.Empty<PrivateVoidUser>())
            {
                yield return u;
            }
        }

        return new()
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalResults = totalUsers,
            Results = Enumerate()
        };
    }

    public async ValueTask UpdateProfile(PublicVoidUser newUser)
    {
        await _connection.ExecuteAsync(
            @"update ""Users"" set ""DisplayName"" = @displayName, ""Avatar"" = @avatar where ""Id"" = :id",
            new {id = newUser.Id, displayName = newUser.DisplayName, avatar = newUser.Avatar});
    }
}