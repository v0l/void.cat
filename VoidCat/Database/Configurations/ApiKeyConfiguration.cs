using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VoidCat.Database.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKey");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Token)
            .IsRequired();

        builder.Property(a => a.Expiry)
            .IsRequired();

        builder.Property(a => a.Created)
            .IsRequired();

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId);
    }
}
