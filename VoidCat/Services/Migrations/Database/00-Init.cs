using System.Data;
using FluentMigrator;
using VoidCat.Model;

namespace VoidCat.Services.Migrations.Database;

[Migration(20220604_2232)]
public class Init : Migration
{
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Email").AsString().NotNullable().Indexed()
            .WithColumn("Password").AsString()
            .WithColumn("Created").AsDateTimeOffset().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("LastLogin").AsDateTimeOffset().Nullable()
            .WithColumn("Avatar").AsString().Nullable()
            .WithColumn("DisplayName").AsString().WithDefaultValue("void user")
            .WithColumn("Flags").AsInt32().WithDefaultValue((int) VoidUserFlags.PublicProfile);

        Create.Table("Files")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString().Nullable()
            .WithColumn("Size").AsInt64()
            .WithColumn("Uploaded").AsDateTimeOffset().Indexed().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("Description").AsString().Nullable()
            .WithColumn("MimeType").AsString().WithDefaultValue("application/octet-stream")
            .WithColumn("Digest").AsString().Nullable()
            .WithColumn("EditSecret").AsGuid();

        Create.Table("UserFiles")
            .WithColumn("File").AsGuid().ForeignKey("Files", "Id").OnDelete(Rule.Cascade).Indexed()
            .WithColumn("User").AsGuid().ForeignKey("Users", "Id").OnDelete(Rule.Cascade).Indexed();

        Create.UniqueConstraint()
            .OnTable("UserFiles")
            .Columns("File", "User");

        Create.Table("Paywall")
            .WithColumn("File").AsGuid().ForeignKey("Files", "Id").OnDelete(Rule.Cascade).PrimaryKey()
            .WithColumn("Service").AsInt16()
            .WithColumn("Currency").AsInt16()
            .WithColumn("Amount").AsDecimal();

        Create.Table("PaywallStrike")
            .WithColumn("File").AsGuid().ForeignKey("Paywall", "File").OnDelete(Rule.Cascade).PrimaryKey()
            .WithColumn("Handle").AsString();

        Create.Table("PaywallOrder")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("File").AsGuid().ForeignKey("Files", "Id").OnDelete(Rule.Cascade).Indexed()
            .WithColumn("Service").AsInt16()
            .WithColumn("Currency").AsInt16()
            .WithColumn("Amount").AsDecimal()
            .WithColumn("Status").AsInt16().Indexed();

        Create.Table("PaywallOrderLightning")
            .WithColumn("Order").AsGuid().ForeignKey("PaywallOrder", "Id").OnDelete(Rule.Cascade).PrimaryKey()
            .WithColumn("Invoice").AsString()
            .WithColumn("Expire").AsDateTimeOffset();
        
        Create.Table("UserRoles")
            .WithColumn("User").AsGuid().ForeignKey("Users", "Id").OnDelete(Rule.Cascade).Indexed()
            .WithColumn("Role").AsString().NotNullable();

        Create.UniqueConstraint()
            .OnTable("UserRoles")
            .Columns("User", "Role");

        Create.Table("EmailVerification")
            .WithColumn("User").AsGuid().ForeignKey("Users", "Id").OnDelete(Rule.Cascade)
            .WithColumn("Code").AsGuid()
            .WithColumn("Expires").AsDateTimeOffset();

        Create.UniqueConstraint()
            .OnTable("EmailVerification")
            .Columns("User", "Code");

        Create.Table("VirusScanResult")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("File").AsGuid().ForeignKey("Files", "Id").OnDelete(Rule.Cascade).Indexed()
            .WithColumn("ScanTime").AsDateTimeOffset().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("Scanner").AsString()
            .WithColumn("Score").AsDecimal()
            .WithColumn("Names").AsString().Nullable();
    }

    public override void Down()
    {
        Delete.Table("Users");
        Delete.Table("Files");
        Delete.Table("UsersFiles");
        Delete.Table("Paywall");
        Delete.Table("PaywallStrike");
        Delete.Table("UserRoles");
        Delete.Table("EmailVerification");
        Delete.Table("VirusScanResult");
    }
}