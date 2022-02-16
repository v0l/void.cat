using System.Buffers;
using System.Security.Cryptography;
using VoidCat.Model;
using VoidCat.Model.Exceptions;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services;

public class LocalDiskFileStore : IFileStore
{
    private readonly VoidSettings _settings;
    private readonly IStatsCollector _stats;
    private readonly IFileMetadataStore _metadataStore;

    public LocalDiskFileStore(VoidSettings settings, IStatsCollector stats,
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

    public async ValueTask<VoidFile?> Get(Guid id)
    {
        return await _metadataStore.Get(id);
    }

    public async ValueTask Egress(EgressRequest request, Stream outStream, CancellationToken cts)
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

    public async ValueTask<InternalVoidFile> Ingress(IngressPayload payload, CancellationToken cts)
    {
        var id = payload.Id ?? Guid.NewGuid();
        var fPath = MapPath(id);
        InternalVoidFile? vf = null;
        if (payload.IsAppend)
        {
            vf = await _metadataStore.Get(payload.Id!.Value);
            if (vf?.EditSecret != null && vf.EditSecret != payload.EditSecret)
            {
                throw new VoidNotAllowedException("Edit secret incorrect!");
            }
        }

        // open file
        await using var fsTemp = new FileStream(fPath,
            payload.IsAppend ? FileMode.Append : FileMode.Create, FileAccess.Write);

        var (total, hash) = await IngressInternal(id, payload.InStream, fsTemp, cts);

        if (!hash.Equals(payload.Hash, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new CryptographicException("Invalid file hash");
        }

        if (payload.IsAppend)
        {
            vf = vf! with
            {
                Size = vf.Size + total
            };
        }
        else
        {
            vf = new InternalVoidFile()
            {
                Id = id,
                Metadata = payload.Meta,
                Uploaded = DateTimeOffset.UtcNow,
                EditSecret = Guid.NewGuid(),
                Size = total
            };
        }


        await _metadataStore.Set(vf);
        return vf;
    }

    public ValueTask UpdateInfo(VoidFile patch, Guid editSecret)
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

    private async Task<(ulong, string)> IngressInternal(Guid id, Stream ingress, Stream fs, CancellationToken cts)
    {
        using var buffer = MemoryPool<byte>.Shared.Rent();
        var total = 0UL;
        var readLength = 0;
        var sha = SHA256.Create();
        while ((readLength = await ingress.ReadAsync(buffer.Memory, cts)) > 0)
        {
            var buf = buffer.Memory[..readLength];
            await fs.WriteAsync(buf, cts);
            await _stats.TrackIngress(id, (ulong) readLength);
            sha.TransformBlock(buf.ToArray(), 0, buf.Length, null, 0);
            total += (ulong) readLength;
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return (total, BitConverter.ToString(sha.Hash!).Replace("-", string.Empty));
    }

    private async Task EgressFull(Guid id, FileStream fileStream, Stream outStream,
        CancellationToken cts)
    {
        using var buffer = MemoryPool<byte>.Shared.Rent();
        var readLength = 0;
        while ((readLength = await fileStream.ReadAsync(buffer.Memory, cts)) > 0)
        {
            await outStream.WriteAsync(buffer.Memory[..readLength], cts);
            await _stats.TrackEgress(id, (ulong) readLength);
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
                await outStream.WriteAsync(buffer.Memory[..(int) toWrite], cts);
                await _stats.TrackEgress(id, (ulong) toWrite);
                dataRemaining -= toWrite;
                await outStream.FlushAsync(cts);
            }
        }
    }

    private string MapPath(Guid id) =>
        Path.Join(_settings.DataDirectory, id.ToString());
}