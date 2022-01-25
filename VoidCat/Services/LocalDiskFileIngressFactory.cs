using System.Buffers;
using VoidCat.Model;

namespace VoidCat.Services;

public class LocalDiskFileIngressFactory : IFileIngressFactory
{
    private readonly VoidSettings _settings;
    private readonly IStatsCollector _stats;

    public LocalDiskFileIngressFactory(VoidSettings settings, IStatsCollector stats)
    {
        _settings = settings;
        _stats = stats;
    }

    public async Task<VoidFile> Ingress(Stream inStream)
    {
        var id = Guid.NewGuid();
        var fPath = mapPath(id);
        using var fsTemp = new FileStream(fPath, FileMode.Create, FileAccess.ReadWrite);

        var buffer = MemoryPool<byte>.Shared.Rent();
        var total = 0UL;
        var readLength = 0;
        while ((readLength = await inStream.ReadAsync(buffer.Memory)) > 0)
        {
            await fsTemp.WriteAsync(buffer.Memory[..readLength]);
            await _stats.TrackIngress(id, (ulong)readLength);
            total += (ulong)readLength;
        }
        
        return new()
        {
            Id = id,
            Size = total
        };
    }

    private string mapPath(Guid id) =>
        Path.Join(_settings.FilePath, id.ToString());
}