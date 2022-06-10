﻿using Dapper;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

/// <inheritdoc />
public class PostgresUserStore : IUserStore
{
    private readonly PostgresConnectionFactory _connection;

    public PostgresUserStore(PostgresConnectionFactory connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public async ValueTask<VoidUser?> Get(Guid id)
    {
        return await Get<PublicVoidUser>(id);
    }

    /// <inheritdoc />
    public async ValueTask<InternalVoidUser?> GetPrivate(Guid id)
    {
        return await Get<InternalVoidUser>(id);
    }

    /// <inheritdoc />
    public async ValueTask Set(Guid id, InternalVoidUser obj)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(
            @"insert into 
""Users""(""Id"", ""Email"", ""Password"", ""LastLogin"", ""DisplayName"", ""Avatar"", ""Flags"") 
values(:id, :email, :password, :lastLogin, :displayName, :avatar, :flags)",
            new
            {
                Id = id,
                email = obj.Email,
                password = obj.Password,
                displayName = obj.DisplayName,
                lastLogin = obj.LastLogin.ToUniversalTime(),
                avatar = obj.Avatar,
                flags = (int) obj.Flags
            });
        if (obj.Roles.Any(a => a != Roles.User))
        {
            foreach (var r in obj.Roles.Where(a => a != Roles.User))
            {
                await conn.ExecuteAsync(
                    @"insert into ""UserRoles""(""User"", ""Role"") values(:user, :role)",
                    new {user = obj.Id, role = r});
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask Delete(Guid id)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(@"delete from ""Users"" where ""Id"" = :id", new {id});
    }

    /// <inheritdoc />
    public async ValueTask<T?> Get<T>(Guid id) where T : VoidUser
    {
        await using var conn = await _connection.Get();
        var user = await conn.QuerySingleOrDefaultAsync<T?>(@"select * from ""Users"" where ""Id"" = :id",
            new {id});
        if (user != default)
        {
            var roles = await conn.QueryAsync<string>(
                @"select ""Role"" from ""UserRoles"" where ""User"" = :id",
                new {id});
            foreach (var r in roles)
            {
                user.Roles.Add(r);
            }
        }

        return user;
    }

    /// <inheritdoc />
    public async ValueTask<Guid?> LookupUser(string email)
    {
        await using var conn = await _connection.Get();
        return await conn.QuerySingleOrDefaultAsync<Guid?>(
            @"select ""Id"" from ""Users"" where ""Email"" = :email",
            new {email});
    }

    /// <inheritdoc />
    public async ValueTask<PagedResult<PrivateVoidUser>> ListUsers(PagedRequest request)
    {
        await using var conn = await _connection.Get();
        var totalUsers = await conn.ExecuteScalarAsync<int>(@"select count(*) from ""Users""");

        async IAsyncEnumerable<PrivateVoidUser> Enumerate()
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
            await using var iconn = await _connection.Get();
            var users = await iconn.ExecuteReaderAsync(
                $@"select * from ""Users"" order by ""{orderBy}"" {sortBy} offset :offset limit :limit",
                new
                {
                    offset = request.PageSize * request.Page,
                    limit = request.PageSize
                });
            var rowParser = users.GetRowParser<PrivateVoidUser>();
            while (await users.ReadAsync())
            {
                yield return rowParser(users);
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

    /// <inheritdoc />
    public async ValueTask UpdateProfile(PublicVoidUser newUser)
    {
        var oldUser = await Get<InternalVoidUser>(newUser.Id);
        if (oldUser == null) return;

        var emailFlag = oldUser.Flags.HasFlag(VoidUserFlags.EmailVerified) ? VoidUserFlags.EmailVerified : 0;
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(
            @"update ""Users"" set ""DisplayName"" = @displayName, ""Avatar"" = @avatar, ""Flags"" = :flags where ""Id"" = :id",
            new
            {
                id = newUser.Id,
                displayName = newUser.DisplayName,
                avatar = newUser.Avatar,
                flags = newUser.Flags | emailFlag
            });
    }

    /// <inheritdoc />
    public async ValueTask UpdateLastLogin(Guid id, DateTime timestamp)
    {
        await using var conn = await _connection.Get();
        await conn.ExecuteAsync(@"update ""Users"" set ""LastLogin"" = :timestamp where ""Id"" = :id",
            new {id, timestamp});
    }
}