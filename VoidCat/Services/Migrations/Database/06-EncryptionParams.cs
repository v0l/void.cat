using FluentMigrator;

namespace VoidCat.Services.Migrations.Database;

[Migration(20220911_1635)]
public class EncryptionParams  : Migration{
    public override void Up()
    {
        Create.Column("EncryptionParams")
            .OnTable("Files")
            .AsString()
            .Nullable();
    }

    public override void Down()
    {
        Delete.Column("EncryptionParams")
            .FromTable("Files");
    }
}