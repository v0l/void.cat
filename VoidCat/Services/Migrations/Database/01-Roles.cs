using FluentMigrator;

namespace VoidCat.Services.Migrations.Database;

[Migration(20220608_1345)]
public class UserRoles : Migration
{
    public override void Up()
    {
        Create.Table("UserRoles")
            .WithColumn("User").AsGuid().ForeignKey("Users", "Id").PrimaryKey()
            .WithColumn("Role").AsString().NotNullable();

        Create.UniqueConstraint()
            .OnTable("UserRoles")
            .Columns("User", "Role");
    }

    public override void Down()
    {
        Delete.Table("UserRoles");
    }
}