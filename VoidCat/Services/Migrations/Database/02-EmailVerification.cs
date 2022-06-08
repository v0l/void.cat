using FluentMigrator;

namespace VoidCat.Services.Migrations.Database;

[Migration(20220608_1443)]
public class EmailVerification : Migration
{
    public override void Up()
    {
        Create.Table("EmailVerification")
            .WithColumn("User").AsGuid().ForeignKey("Users", "Id")
            .WithColumn("Code").AsGuid()
            .WithColumn("Expires").AsDateTime();

        Create.UniqueConstraint()
            .OnTable("EmailVerification")
            .Columns("User", "Code");
    }

    public override void Down()
    {
        Delete.Table("EmailVerification");
    }
}