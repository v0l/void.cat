using System.Data;
using FluentMigrator;

namespace VoidCat.Services.Migrations.Database;

[Migration(20220725_1137)]
public class MinorVersion1 : Migration
{
    public override void Up()
    {
        Create.Table("ApiKey")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().ForeignKey("Users", "Id").OnDelete(Rule.Cascade).Indexed()
            .WithColumn("Token").AsString()
            .WithColumn("Expiry").AsDateTimeOffset()
            .WithColumn("Created").AsDateTimeOffset().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Column("Storage")
            .OnTable("Files")
            .AsString().WithDefaultValue("local-disk");

        Create.Column("Storage")
            .OnTable("Users")
            .AsString().WithDefaultValue("local-disk");
    }
    
    public override void Down()
    {
        Delete.Table("ApiKey");
        
        Delete.Column("Storage")
            .FromTable("Files");

        Delete.Column("Storage")
            .FromTable("Users");
    }
}
