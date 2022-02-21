using Newtonsoft.Json;
using VoidCat.Model;

namespace VoidCat.Services.Migrations;

public class FixMigration_20220218 : MetadataMigrator<WrongFile, WrongMeta>
{
    public FixMigration_20220218(VoidSettings settings, ILogger<MetadataMigrator<WrongFile, WrongMeta>> logger) : base(settings, logger)
    {
    }
    
    protected override string OldPath => "metadata-v2";
    protected override string NewPath => "metadata-v3";
    
    protected override bool ShouldMigrate(string json)
    {
        var metaBase = JsonConvert.DeserializeObject<WrongFile>(json);
        return metaBase?.Metadata?.Version == 2 && metaBase?.Metadata?.Size > 0;
    }

    protected override WrongMeta MigrateModel(WrongFile old)
    {
        return old.Metadata!;
    }
}

public class WrongFile
{
    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid Id { get; init; }
    public WrongMeta? Metadata { get; init; }
}

public class WrongMeta
{
    public int Version { get; init; } = 2;
    public string? Name { get; init; }
    public ulong Size { get; init; }
    public DateTimeOffset Uploaded { get; init; } = DateTimeOffset.UtcNow;
    public string? Description { get; init; }
    public string? MimeType { get; init; }
    public string? Digest { get; init; }
    [JsonConverter(typeof(Base58GuidConverter))]
    public Guid EditSecret { get; init; }
}