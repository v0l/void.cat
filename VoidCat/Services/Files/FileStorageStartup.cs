using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Users;

namespace VoidCat.Services.Files;

public static class FileStorageStartup
{
    public static void AddStorage(this IServiceCollection services, VoidSettings settings)
    {
        services.AddTransient<IFileInfoManager, FileInfoManager>();
        services.AddTransient<IUserUploadsStore, UserUploadStore>();

        if (settings.CloudStorage != default)
        {
            // cloud storage
            services.AddSingleton<IFileStore, S3FileStore>();
            services.AddSingleton<IFileMetadataStore, S3FileMetadataStore>();
        }
        else
        {
            services.AddTransient<IFileStore, LocalDiskFileStore>();
            services.AddTransient<IFileMetadataStore, LocalDiskFileMetadataStore>();
        }
    }
}