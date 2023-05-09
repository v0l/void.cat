using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VoidCat.Database.Configurations;

public class PaywallOrderLightningConfiguration : IEntityTypeConfiguration<PaywallOrderLightning>
{
    public void Configure(EntityTypeBuilder<PaywallOrderLightning> builder)
    {
        builder.ToTable("PaymentOrderLightning");
        builder.HasKey(a => a.OrderId);
        builder.Property(a => a.Invoice)
            .IsRequired();

        builder.Property(a => a.Expire)
            .IsRequired();

        builder.HasOne(a => a.Order)
            .WithOne(a => a.OrderLightning)
            .HasForeignKey<PaywallOrderLightning>(a => a.OrderId);
    }
}
