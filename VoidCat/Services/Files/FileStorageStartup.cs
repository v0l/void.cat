using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Users;

namespace VoidCat.Services.Files;

public static class FileStorageStartup
{
    public static void AddStorage(this IServiceCollection services, VoidSettings settings)
    {
        services.AddTransient<IFileInfoManager, FileInfoManager>();

        if (settings.CloudStorage != default)
        {
            services.AddTransient<IUserUploadsStore, UserUploadStore>();
            
            // cloud storage
            if (settings.CloudStorage.S3 != default)
            {
                services.AddSingleton<IFileStore, S3FileStore>();
                services.AddSingleton<IFileMetadataStore, S3FileMetadataStore>();
            }
        }
        else if (!string.IsNullOrEmpty(settings.Postgres))
        {
            services.AddTransient<IUserUploadsStore, PostgresUserUploadStore>();
            services.AddTransient<IFileStore, LocalDiskFileStore>();
            services.AddTransient<IFileMetadataStore, PostgresFileMetadataStore>();
        }
        else
        {
            services.AddTransient<IUserUploadsStore, UserUploadStore>();
            services.AddTransient<IFileStore, LocalDiskFileStore>();
            services.AddTransient<IFileMetadataStore, LocalDiskFileMetadataStore>();
        }
    }
}