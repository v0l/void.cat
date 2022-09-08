using System.Data;
using FluentMigrator;

namespace VoidCat.Services.Migrations.Database;

[Migration(20220907_2015)]
public class AccountTypes : Migration
{
    public override void Up()
    {
        Create.Column("AuthType")
            .OnTable("Users")
            .AsInt16()
            .WithDefaultValue(0);

        Alter.Column("Password")
            .OnTable("Users")
            .AsString()
            .Nullable();

        Create.Table("UsersAuthToken")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("User").AsGuid().ForeignKey("Users", "Id").OnDelete(Rule.Cascade).Indexed()
            .WithColumn("Provider").AsString()
            .WithColumn("AccessToken").AsString()
            .WithColumn("TokenType").AsString()
            .WithColumn("Expires").AsDateTimeOffset()
            .WithColumn("RefreshToken").AsString()
            .WithColumn("Scope").AsString();
    }

    public override void Down()
    {
        Delete.Column("Type")
            .FromTable("Users");

        Alter.Column("Password")
            .OnTable("Users")
            .AsString()
            .NotNullable();

        Delete.Table("UsersAuthToken");
    }
}