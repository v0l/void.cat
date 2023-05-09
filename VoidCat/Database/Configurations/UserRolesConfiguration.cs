using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VoidCat.Database.Configurations;

public class UserRolesConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(a => new {a.UserId, a.Role});
        
        builder.Property(a => a.Role)
            .IsRequired();
        
        builder.HasOne(a => a.User)
            .WithMany(a => a.Roles)
            .HasForeignKey(a => a.UserId);
    }
}
