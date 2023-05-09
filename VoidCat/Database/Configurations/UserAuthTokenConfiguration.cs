using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VoidCat.Database.Configurations;

public class UserAuthTokenConfiguration : IEntityTypeConfiguration<UserAuthToken>
{
    public void Configure(EntityTypeBuilder<UserAuthToken> builder)
    {
        builder.ToTable("UsersAuthToken");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Provider)
            .IsRequired();

        builder.Property(a => a.AccessToken)
            .IsRequired();

        builder.Property(a => a.TokenType)
            .IsRequired();

        builder.Property(a => a.Expires)
            .IsRequired();

        builder.Property(a => a.RefreshToken)
            .IsRequired();

        builder.Property(a => a.Scope)
            .IsRequired();

        builder.Property(a => a.IdToken);
        
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId);
    }
}
