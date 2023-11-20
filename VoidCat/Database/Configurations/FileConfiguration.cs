using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VoidCat.Database.Configurations;

public class FileConfiguration : IEntityTypeConfiguration<File>
{
    public void Configure(EntityTypeBuilder<File> builder)
    {
        builder.ToTable("Files");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name);
        builder.Property(a => a.Size)
            .IsRequired();

        builder.Property(a => a.Uploaded)
            .IsRequired();

        builder.Property(a => a.Description);
        builder.Property(a => a.MimeType)
            .IsRequired()
            .HasDefaultValue("application/octet-stream");

        builder.Property(a => a.Digest);
        builder.Property(a => a.EditSecret)
            .IsRequired();

        builder.Property(a => a.Expires);

        builder.Property(a => a.Storage)
            .IsRequired()
            .HasDefaultValue("local-disk");

        builder.Property(a => a.EncryptionParams);
        builder.Property(a => a.MagnetLink);

        builder.Property(a => a.OriginalDigest);

        builder.Property(a => a.MediaDimensions);

        builder.HasIndex(a => a.Uploaded);

        builder.HasIndex(a => a.Digest);
        builder.HasIndex(a => a.OriginalDigest);
    }
}
