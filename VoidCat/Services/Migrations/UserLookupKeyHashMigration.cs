using Newtonsoft.Json;
using StackExchange.Redis;
using VoidCat.Model;

namespace VoidCat.Services.Migrations;

public class UserLookupKeyHashMigration : IMigration
{
    private readonly IDatabase _database;

    public UserLookupKeyHashMigration(IDatabase database)
    {
        _database = database;
    }

    public async ValueTask Migrate()
    {
        var users = await _database.SetMembersAsync("users");
        foreach (var userId in users)
        {
            if (!Guid.TryParse(userId, out var gid)) continue;

            var userJson = await _database.StringGetAsync($"user:{gid}");
            var user = JsonConvert.DeserializeObject<UserEmail>(userJson);
            if (user == default) continue;

            if (await _database.KeyExistsAsync(MapOld(user.Email)))
            {
                await _database.KeyDeleteAsync(MapOld(user.Email));
                await _database.StringSetAsync(MapNew(user.Email), $"\"{userId}\"");
            }
        }
    }

    private static RedisKey MapOld(string email) => $"user:email:{email}";
    private static RedisKey MapNew(string email) => $"user:email:{email.Hash("md5")}";

    internal class UserEmail
    {
        public UserEmail(string email) => Email = email;

        public string Email { get; init; }
    }
}