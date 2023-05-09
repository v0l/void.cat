using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VoidCat.Database.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Email)
            .IsRequired();

        builder.Property(a => a.Password)
            .IsRequired(false);

        builder.Property(a => a.Created)
            .IsRequired();

        builder.Property(a => a.LastLogin);
        builder.Property(a => a.DisplayName)
            .IsRequired()
            .HasDefaultValue("void user");

        builder.Property(a => a.Flags)
            .IsRequired();

        builder.Property(a => a.Storage)
            .IsRequired()
            .HasDefaultValue("local-disk");

        builder.Property(a => a.AuthType)
            .IsRequired();

        builder.HasIndex(a => a.Email);
    }
}
