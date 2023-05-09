using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VoidCat.Database.Configurations;

public class UserFileConfiguration : IEntityTypeConfiguration<UserFile>
{
    public void Configure(EntityTypeBuilder<UserFile> builder)
    {
        builder.ToTable("UserFiles");
        builder.HasKey(a => new {a.UserId, a.FileId});
        
        builder.HasOne(a => a.User)
            .WithMany(a => a.UserFiles)
            .HasForeignKey(a => a.UserId);

        builder.HasOne(a => a.File)
            .WithOne()
            .HasForeignKey<UserFile>(a => a.FileId);
    }
}
