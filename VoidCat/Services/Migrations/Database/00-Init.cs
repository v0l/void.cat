using FluentMigrator;
using VoidCat.Model;

namespace VoidCat.Services.Migrations.Database;

[Migration(20220604_2232)]
public class Init : Migration {
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Email").AsString().NotNullable().Indexed()
            .WithColumn("Password").AsString()
            .WithColumn("Created").AsDateTime().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("LastLogin").AsDateTime().Nullable()
            .WithColumn("Avatar").AsString().Nullable()
            .WithColumn("DisplayName").AsString().WithDefaultValue("void user")
            .WithColumn("Flags").AsInt32().WithDefaultValue((int)VoidUserFlags.PublicProfile);

        Create.Table("Files")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString()
            .WithColumn("Size").AsInt64()
            .WithColumn("Uploaded").AsDateTime().Indexed().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("Description").AsString().Nullable()
            .WithColumn("MimeType").AsString().WithDefaultValue("application/octet-stream")
            .WithColumn("Digest").AsString()
            .WithColumn("EditSecret").AsGuid();

        Create.Table("UserFiles")
            .WithColumn("File").AsGuid().ForeignKey("Files", "Id")
            .WithColumn("User").AsGuid().ForeignKey("Users", "Id").Indexed();

        Create.UniqueConstraint()
            .OnTable("UserFiles")
            .Columns("File", "User");

        Create.Table("Paywall")
            .WithColumn("File").AsGuid().ForeignKey("Files", "Id").Unique()
            .WithColumn("Type").AsInt16()
            .WithColumn("Currency").AsInt16()
            .WithColumn("Amount").AsDecimal();

        Create.Table("PaywallStrike")
            .WithColumn("File").AsGuid().ForeignKey("Files", "Id").Unique()
            .WithColumn("Handle").AsString();
    }

    public override void Down()
    {
        Delete.Table("Users");
        Delete.Table("Files");
        Delete.Table("UsersFiles");
        Delete.Table("Paywall");
        Delete.Table("PaywallStrike");
    }
}