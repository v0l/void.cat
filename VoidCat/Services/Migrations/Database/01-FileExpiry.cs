using FluentMigrator;

namespace VoidCat.Services.Migrations.Database;

[Migration(20220615_2238)]
public class FileExpiry : Migration {
    public override void Up()
    {
        Create.Column("Expires")
            .OnTable("Files")
            .AsDateTimeOffset().Nullable().Indexed();
    }

    public override void Down()
    {
        Delete.Column("Expires")
            .FromTable("Files");
    }
}