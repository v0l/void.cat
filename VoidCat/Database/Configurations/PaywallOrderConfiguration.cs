using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VoidCat.Database.Configurations;

public class PaywallOrderConfiguration : IEntityTypeConfiguration<PaywallOrder>
{
    public void Configure(EntityTypeBuilder<PaywallOrder> builder)
    {
        builder.ToTable("PaymentOrder");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Service)
            .IsRequired();

        builder.Property(a => a.Currency)
            .IsRequired();

        builder.Property(a => a.Amount)
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired();

        builder.HasIndex(a => a.Status);

        builder.HasOne(a => a.File)
            .WithMany()
            .HasForeignKey(a => a.FileId);
    }
}
