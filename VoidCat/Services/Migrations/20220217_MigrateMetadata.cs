using Newtonsoft.Json;
using VoidCat.Model;

namespace VoidCat.Services.Migrations;

public class MigrateMetadata_20220217 : IMigration
{
    private const string MetadataDir = "metadata";
    private const string MetadataV2Dir = "metadata-v2";
    private readonly ILogger<MigrateMetadata_20220217> _logger;
    private readonly VoidSettings _settings;

    public MigrateMetadata_20220217(VoidSettings settings, ILogger<MigrateMetadata_20220217> log)
    {
        _settings = settings;
        _logger = log;
    }

    public async ValueTask Migrate()
    {
        var newMeta = Path.Combine(_settings.DataDirectory, MetadataV2Dir);
        if (!Directory.Exists(newMeta))
        {
            Directory.CreateDirectory(newMeta);
        }
        
        foreach (var fe in Directory.EnumerateFiles(_settings.DataDirectory))
        {
            var filename = Path.GetFileNameWithoutExtension(fe);
            if (!Guid.TryParse(filename, out var id)) continue;

            var fp = MapMeta(id);
            if (File.Exists(fp))
            {
                _logger.LogInformation("Migrating metadata for {file}", fp);
                try
                {
                    var oldJson = await File.ReadAllTextAsync(fp);
                    if(!oldJson.Contains("\"Metadata\":")) continue; // old format should contain "Metadata":
                    
                    var old = JsonConvert.DeserializeObject<InternalVoidFile>(oldJson);
                    var newObj = new PrivateVoidFile()
                    {
                        Id = old!.Id,
                        Metadata = new()
                        {
                            Name = old.Metadata!.Name,
                            Description = old.Metadata.Description,
                            Uploaded = old.Uploaded,
                            MimeType = old.Metadata.MimeType,
                            EditSecret = old.EditSecret,
                            Size = old.Size
                        }
                    };

                    await File.WriteAllTextAsync(MapV2Meta(id), JsonConvert.SerializeObject(newObj));
                    
                    // delete old metadata
                    File.Delete(fp);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
        }
    }
    
    private string MapMeta(Guid id) =>
        Path.ChangeExtension(Path.Join(_settings.DataDirectory, MetadataDir, id.ToString()), ".json");
    private string MapV2Meta(Guid id) =>
        Path.ChangeExtension(Path.Join(_settings.DataDirectory, MetadataV2Dir, id.ToString()), ".json");
    
    private record VoidFile
    {
        [JsonConverter(typeof(Base58GuidConverter))]
        public Guid Id { get; init; }

        public VoidFileMeta? Metadata { get; set; }
        
        public ulong Size { get; init; }

        public DateTimeOffset Uploaded { get; init; }
    }
    
    private record InternalVoidFile : VoidFile
    {
        [JsonConverter(typeof(Base58GuidConverter))]
        public Guid EditSecret { get; init; }
    }

    private record VoidFileMeta
    {
        public string? Name { get; init; }

        public string? Description { get; init; }
        
        public string? MimeType { get; init; }
    }
    
    private record NewVoidFileMeta
    {
        public string? Name { get; init; }
        public ulong Size { get; init; }
        public DateTimeOffset Uploaded { get; init; } = DateTimeOffset.UtcNow;
        public string? Description { get; init; }
        public string? MimeType { get; init; }
        public string? Digest { get; init; }
    }

    private record NewSecretVoidFileMeta : NewVoidFileMeta
    {
        [JsonConverter(typeof(Base58GuidConverter))]
        public Guid EditSecret { get; init; }
    }
}