using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Users;

namespace VoidCat.Services.Files;

public static class FileStorageStartup
{
    public static void AddStorage(this IServiceCollection services, VoidSettings settings)
    {
        services.AddTransient<FileInfoManager>();
        services.AddTransient<FileStoreFactory>();
        services.AddTransient<StripMetadata>();
        
        if (settings.CloudStorage != default)
        {
            // S3 storage
            foreach (var s3 in settings.CloudStorage.S3 ?? Array.Empty<S3BlobConfig>())
            {
                services.AddTransient<IFileStore>((svc) =>
                    new S3FileStore(s3, 
                        svc.GetRequiredService<IAggregateStatsCollector>(),
                        svc.GetRequiredService<FileInfoManager>(),
                        svc.GetRequiredService<ICache>()));

                if (settings.MetadataStore == s3.Name)
                {
                    services.AddSingleton<IFileMetadataStore>((svc) =>
                        new S3FileMetadataStore(s3, svc.GetRequiredService<ILogger<S3FileMetadataStore>>()));
                }
            }
        }

        if (settings.HasPostgres())
        {
            services.AddTransient<IUserUploadsStore, PostgresUserUploadStore>();
            services.AddTransient<IFileStore, LocalDiskFileStore>();
            if (settings.MetadataStore is "postgres" or "local-disk")
            {
                services.AddSingleton<IFileMetadataStore, PostgresFileMetadataStore>();
            }
        }
        else
        {
            services.AddTransient<IUserUploadsStore, CacheUserUploadStore>();
            services.AddTransient<IFileStore, LocalDiskFileStore>();
            if (settings.MetadataStore == "local-disk")
            {
                services.AddSingleton<IFileMetadataStore, LocalDiskFileMetadataStore>();
            }
        }
    }
}
