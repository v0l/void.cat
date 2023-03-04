using FluentMigrator;

namespace VoidCat.Services.Migrations.Database;

[Migration(20230304_1509)]
public class MagnetLink : Migration {
    public override void Up()
    {
        Create.Column("MagnetLink")
            .OnTable("Files")
            .AsString()
            .Nullable();
    }
    
    public override void Down()
    {
        Delete.Column("MagnetLink")
            .FromTable("Files");
    }
}
