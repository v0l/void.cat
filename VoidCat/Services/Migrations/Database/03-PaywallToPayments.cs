using FluentMigrator;

namespace VoidCat.Services.Migrations.Database;

[Migration(20220908_1527)]
public class PaywallToPayments : Migration
{
    public override void Up()
    {
        Rename.Table("Paywall")
            .To("Payment");

        Rename.Table("PaywallOrder")
            .To("PaymentOrder");

        Rename.Table("PaywallStrike")
            .To("PaymentStrike");

        Rename.Table("PaywallOrderLightning")
            .To("PaymentOrderLightning");
    }

    public override void Down()
    {
        // yolo
    }
}