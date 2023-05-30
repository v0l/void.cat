using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VoidCat.Database.Configurations;

public class PaywallConfiguration : IEntityTypeConfiguration<Paywall>
{
    public void Configure(EntityTypeBuilder<Paywall> builder)
    {
        builder.ToTable("Payment");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Service)
            .IsRequired();

        builder.Property(a => a.Currency)
            .IsRequired();

        builder.Property(a => a.Amount)
            .IsRequired();

        builder.Property(a => a.Required)
            .IsRequired();

        builder.HasOne(a => a.File)
            .WithOne(a => a.Paywall)
            .HasForeignKey<Paywall>(a => a.FileId);

        builder.Property(a => a.Upstream);
    }
}
