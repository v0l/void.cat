using Microsoft.EntityFrameworkCore;

namespace VoidCat.Services.Migrations;

public class EFMigration : IMigration
{
    private readonly VoidContext _db;
    
    public EFMigration(VoidContext db)
    {
        _db = db;
    }
    
    public int Order => 0;
    
    public async ValueTask<IMigration.MigrationResult> Migrate(string[] args)
    {
        await _db.Database.MigrateAsync();
        return IMigration.MigrationResult.Completed;
    }
}
