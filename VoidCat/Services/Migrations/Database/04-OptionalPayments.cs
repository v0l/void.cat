using FluentMigrator;

namespace VoidCat.Services.Migrations.Database;

[Migration(20220908_1602)]
public class OptionalPayments : Migration{
    public override void Up()
    {
        Create.Column("Required")
            .OnTable("Payment")
            .AsBoolean()
            .WithDefaultValue(true);
    }

    public override void Down()
    {
        Delete.Column("Required")
            .FromTable("Payment");
    }
}