using Newtonsoft.Json;
using VoidCat.Model;

namespace VoidCat.Services.Migrations;

public abstract class MetadataMigrator<TOld, TNew> : IMigration
{
    private readonly VoidSettings _settings;
    private readonly ILogger<MetadataMigrator<TOld, TNew>> _logger;

    protected MetadataMigrator(VoidSettings settings, ILogger<MetadataMigrator<TOld, TNew>> logger)
    {
        _settings = settings;
        _logger = logger;
    }
    
    public async ValueTask Migrate()
    {
        var newMeta = Path.Combine(_settings.DataDirectory, OldPath);
        if (!Directory.Exists(newMeta))
        {
            Directory.CreateDirectory(newMeta);
        }
        
        foreach (var fe in Directory.EnumerateFiles(_settings.DataDirectory))
        {
            var filename = Path.GetFileNameWithoutExtension(fe);
            if (!Guid.TryParse(filename, out var id)) continue;

            var fp = MapOldMeta(id);
            if (File.Exists(fp))
            {
                _logger.LogInformation("Migrating metadata for {file}", fp);
                try
                {
                    var oldJson = await File.ReadAllTextAsync(fp);
                    if (!ShouldMigrate(oldJson)) continue;
                    
                    var old = JsonConvert.DeserializeObject<TOld>(oldJson);
                    if(old == null) continue;
                    
                    var newObj = MigrateModel(old);
                    await File.WriteAllTextAsync(MapNewMeta(id), JsonConvert.SerializeObject(newObj));
                    
                    // delete old metadata
                    File.Delete(fp);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error migrating metadata: {Message}", ex.Message);
                }
            }
        }
    }

    protected abstract string OldPath { get; }
    protected abstract string NewPath { get; }

    protected abstract bool ShouldMigrate(string json);
    protected abstract TNew MigrateModel(TOld old);
    
    private string MapOldMeta(Guid id) =>
        Path.ChangeExtension(Path.Join(_settings.DataDirectory, OldPath, id.ToString()), ".json");
    private string MapNewMeta(Guid id) =>
        Path.ChangeExtension(Path.Join(_settings.DataDirectory, NewPath, id.ToString()), ".json");
}