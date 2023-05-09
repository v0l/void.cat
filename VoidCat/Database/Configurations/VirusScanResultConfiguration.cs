using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VoidCat.Database.Configurations;

public class VirusScanResultConfiguration : IEntityTypeConfiguration<VirusScanResult>
{
    public void Configure(EntityTypeBuilder<VirusScanResult> builder)
    {
        builder.ToTable("VirusScanResult");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Scanner)
            .IsRequired();

        builder.Property(a => a.Score)
            .IsRequired();

        builder.Property(a => a.Names);

        builder.HasOne(a => a.File)
            .WithMany()
            .HasForeignKey(a => a.FileId);
    }
}
