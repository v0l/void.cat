using System.Buffers;
using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Model.Exceptions;

namespace VoidCat.Services;

public class LocalDiskFileIngressFactory : IFileStorage
{
    private readonly VoidSettings _settings;
    private readonly IStatsCollector _stats;
    
    public LocalDiskFileIngressFactory(VoidSettings settings, IStatsCollector stats)
    {
        _settings = settings;
        _stats = stats;

        if (!Directory.Exists(_settings.DataDirectory))
        {
            Directory.CreateDirectory(_settings.DataDirectory);
        }
    }

    public async Task<VoidFile?> Get(Guid id)
    {
        var path = MapMeta(id);
        if (!File.Exists(path)) throw new VoidFileNotFoundException(id);
        
        var json = await File.ReadAllTextAsync(path);
        return JsonConvert.DeserializeObject<VoidFile>(json);
    }

    public async Task Egress(Guid id, Stream outStream, CancellationToken cts)
    {
        var path = MapPath(id);
        if (!File.Exists(path)) throw new VoidFileNotFoundException(id);

        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var buffer = MemoryPool<byte>.Shared.Rent();
        var readLength = 0;
        while ((readLength = await fs.ReadAsync(buffer.Memory, cts)) > 0)
        {
            await outStream.WriteAsync(buffer.Memory[..readLength], cts);
            await _stats.TrackEgress(id, (ulong)readLength);
        }
    }

    public async Task<InternalVoidFile> Ingress(Stream inStream, VoidFileMeta meta, CancellationToken cts)
    {
        var id = Guid.NewGuid();
        var fPath = MapPath(id);
        await using var fsTemp = new FileStream(fPath, FileMode.Create, FileAccess.ReadWrite);

        using var buffer = MemoryPool<byte>.Shared.Rent();
        var total = 0UL;
        var readLength = 0;
        while ((readLength = await inStream.ReadAsync(buffer.Memory, cts)) > 0)
        {
            await fsTemp.WriteAsync(buffer.Memory[..readLength], cts);
            await _stats.TrackIngress(id, (ulong)readLength);
            total += (ulong)readLength;
        }
        
        var fm = new InternalVoidFile()
        {
            Id = id,
            Size = total,
            Metadata = meta,
            Uploaded = DateTimeOffset.UtcNow,
            EditSecret = Guid.NewGuid()
        };
        
        var mPath = MapMeta(id);
        var json = JsonConvert.SerializeObject(fm);
        await File.WriteAllTextAsync(mPath, json, cts);
        return fm;
    }

    public async Task UpdateInfo(VoidFile patch, Guid editSecret)
    {
        var path = MapMeta(patch.Id);
        if (!File.Exists(path)) throw new VoidFileNotFoundException(patch.Id);

        var oldJson = await File.ReadAllTextAsync(path);
        var oldObj = JsonConvert.DeserializeObject<InternalVoidFile>(oldJson);

        if (oldObj?.EditSecret != editSecret)
        {
            throw new VoidNotAllowedException("Edit secret incorrect");
        }

        // only patch metadata
        oldObj.Metadata = patch.Metadata;
        
        var json = JsonConvert.SerializeObject(oldObj);
        await File.WriteAllTextAsync(path, json);
    }

    private string MapPath(Guid id) =>
        Path.Join(_settings.DataDirectory, id.ToString());

    private string MapMeta(Guid id) =>
        Path.ChangeExtension(MapPath(id), ".json");
}