using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VoidCat.Database.Configurations;

public class PaywallStrikeConfiguration : IEntityTypeConfiguration<PaywallStrike>
{
    public void Configure(EntityTypeBuilder<PaywallStrike> builder)
    {
        builder.ToTable("PaymentStrike");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Handle)
            .IsRequired();

        builder.HasOne(a => a.Paywall)
            .WithOne(a => a.PaywallStrike)
            .HasForeignKey<PaywallStrike>();
    }
}
