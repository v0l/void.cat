using System.Buffers;
using VoidCat.Model;
using VoidCat.Model.Exceptions;

namespace VoidCat.Services;

public class LocalDiskFileIngressFactory : IFileStore
{
    private readonly VoidSettings _settings;
    private readonly IStatsCollector _stats;
    private readonly IFileMetadataStore _metadataStore;

    public LocalDiskFileIngressFactory(VoidSettings settings, IStatsCollector stats,
        IFileMetadataStore metadataStore)
    {
        _settings = settings;
        _stats = stats;
        _metadataStore = metadataStore;

        if (!Directory.Exists(_settings.DataDirectory))
        {
            Directory.CreateDirectory(_settings.DataDirectory);
        }
    }

    public async Task<VoidFile?> Get(Guid id)
    {
        return await _metadataStore.Get(id);
    }

    public async Task Egress(EgressRequest request, Stream outStream, CancellationToken cts)
    {
        var path = MapPath(request.Id);
        if (!File.Exists(path)) throw new VoidFileNotFoundException(request.Id);

        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        if (request.Ranges.Any())
        {
            await EgressRanges(request.Id, request.Ranges, fs, outStream, cts);
        }
        else
        {
            await EgressFull(request.Id, fs, outStream, cts);
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

        await _metadataStore.Set(fm);
        return fm;
    }

    public Task UpdateInfo(VoidFile patch, Guid editSecret)
    {
        return _metadataStore.Update(patch, editSecret);
    }

    public async IAsyncEnumerable<VoidFile> ListFiles()
    {
        foreach (var fe in Directory.EnumerateFiles(_settings.DataDirectory))
        {
            var filename = Path.GetFileNameWithoutExtension(fe);
            if (!Guid.TryParse(filename, out var id)) continue;

            var meta = await _metadataStore.Get(id);
            if (meta != default)
            {
                yield return meta;
            }
        }
    }

    private async Task EgressFull(Guid id, FileStream fileStream, Stream outStream,
        CancellationToken cts)
    {
        using var buffer = MemoryPool<byte>.Shared.Rent();
        var readLength = 0;
        while ((readLength = await fileStream.ReadAsync(buffer.Memory, cts)) > 0)
        {
            await outStream.WriteAsync(buffer.Memory[..readLength], cts);
            await _stats.TrackEgress(id, (ulong)readLength);
            await outStream.FlushAsync(cts);
        }
    }

    private async Task EgressRanges(Guid id, IEnumerable<RangeRequest> ranges, FileStream fileStream, Stream outStream,
        CancellationToken cts)
    {
        using var buffer = MemoryPool<byte>.Shared.Rent();
        foreach (var range in ranges)
        {
            fileStream.Seek(range.Start ?? range.End ?? 0L,
                range.Start.HasValue ? SeekOrigin.Begin : SeekOrigin.End);

            var readLength = 0;
            var dataRemaining = range.Size ?? 0L;
            while ((readLength = await fileStream.ReadAsync(buffer.Memory, cts)) > 0
                   && dataRemaining > 0)
            {
                var toWrite = Math.Min(readLength, dataRemaining);
                await outStream.WriteAsync(buffer.Memory[..(int)toWrite], cts);
                await _stats.TrackEgress(id, (ulong)toWrite);
                dataRemaining -= toWrite;
                await outStream.FlushAsync(cts);
            }
        }
    }

    private string MapPath(Guid id) =>
        Path.Join(_settings.DataDirectory, id.ToString());
}
