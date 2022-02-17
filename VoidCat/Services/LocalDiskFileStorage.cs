using System.Buffers;
using System.Security.Cryptography;
using VoidCat.Model;
using VoidCat.Model.Exceptions;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services;

public class LocalDiskFileStore : IFileStore
{
    private const int BufferSize = 1024 * 1024;
    private readonly VoidSettings _settings;
    private readonly IAggregateStatsCollector _stats;
    private readonly IFileMetadataStore _metadataStore;

    public LocalDiskFileStore(VoidSettings settings, IAggregateStatsCollector stats,
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

    public async ValueTask<PublicVoidFile?> Get(Guid id)
    {
        return new ()
        {
            Id = id,
            Metadata = await _metadataStore.GetPublic(id)
        };
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

    public async ValueTask<PrivateVoidFile> Ingress(IngressPayload payload, CancellationToken cts)
    {
        var id = payload.Id ?? Guid.NewGuid();
        var fPath = MapPath(id);
        SecretVoidFileMeta? vf = null;
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
            vf = new SecretVoidFileMeta()
            {
                Name = payload.Meta.Name,
                Description = payload.Meta.Description,
                Digest = payload.Meta.Digest,
                MimeType = payload.Meta.MimeType,
                Uploaded = DateTimeOffset.UtcNow,
                EditSecret = Guid.NewGuid(),
                Size = total
            };
        }


        await _metadataStore.Set(id, vf);
        return new()
        {
            Id = id,
            Metadata = vf
        };
    }

    public async IAsyncEnumerable<PublicVoidFile> ListFiles()
    {
        foreach (var fe in Directory.EnumerateFiles(_settings.DataDirectory))
        {
            var filename = Path.GetFileNameWithoutExtension(fe);
            if (!Guid.TryParse(filename, out var id)) continue;

            var meta = await _metadataStore.Get(id);
            if (meta != default)
            {
                yield return new()
                {
                    Id = id,
                    Metadata = meta
                };
            }
        }
    }

    private async Task<(ulong, string)> IngressInternal(Guid id, Stream ingress, Stream fs, CancellationToken cts)
    {
        using var buffer = MemoryPool<byte>.Shared.Rent(BufferSize);
        var total = 0UL;
        int readLength = 0, offset = 0;
        var sha = SHA256.Create();
        while ((readLength = await ingress.ReadAsync(buffer.Memory[offset..], cts)) > 0 || offset != 0)
        {
            if (readLength != 0 && offset + readLength < buffer.Memory.Length)
            {
                // read until buffer full
                offset += readLength;
                continue;
            }

            var totalRead = readLength + offset;
            var buf = buffer.Memory[..totalRead];
            await fs.WriteAsync(buf, cts);
            await _stats.TrackIngress(id, (ulong) buf.Length);
            sha.TransformBlock(buf.ToArray(), 0, buf.Length, null, 0);
            total += (ulong) buf.Length;
            offset = 0;
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return (total, BitConverter.ToString(sha.Hash!).Replace("-", string.Empty));
    }

    private async Task EgressFull(Guid id, FileStream fileStream, Stream outStream,
        CancellationToken cts)
    {
        using var buffer = MemoryPool<byte>.Shared.Rent(BufferSize);
        int readLength = 0, offset = 0;
        while ((readLength = await fileStream.ReadAsync(buffer.Memory[offset..], cts)) > 0 || offset != 0)
        {
            if (readLength != 0 && offset + readLength < buffer.Memory.Length)
            {
                // read until buffer full
                offset += readLength;
                continue;
            }

            var fullSize = readLength + offset;
            await outStream.WriteAsync(buffer.Memory[..fullSize], cts);
            await _stats.TrackEgress(id, (ulong) fullSize);
            await outStream.FlushAsync(cts);
            offset = 0;
        }
    }

    private async Task EgressRanges(Guid id, IEnumerable<RangeRequest> ranges, FileStream fileStream, Stream outStream,
        CancellationToken cts)
    {
        using var buffer = MemoryPool<byte>.Shared.Rent(BufferSize);
        foreach (var range in ranges)
        {
            fileStream.Seek(range.Start ?? range.End ?? 0L,
                range.Start.HasValue ? SeekOrigin.Begin : SeekOrigin.End);

            int readLength = 0, offset = 0;
            var dataRemaining = range.Size ?? 0L;
            while ((readLength = await fileStream.ReadAsync(buffer.Memory[offset..], cts)) > 0 || offset != 0)
            {
                if (readLength != 0 && offset + readLength < buffer.Memory.Length)
                {
                    // read until buffer full
                    offset += readLength;
                    continue;
                }

                var fullSize = readLength + offset;
                var toWrite = Math.Min(fullSize, dataRemaining);
                await outStream.WriteAsync(buffer.Memory[..(int) toWrite], cts);
                await _stats.TrackEgress(id, (ulong) toWrite);
                await outStream.FlushAsync(cts);
                dataRemaining -= toWrite;
                offset = 0;

                if (dataRemaining == 0)
                {
                    break;
                }
            }
        }
    }

    private string MapPath(Guid id) =>
        Path.Join(_settings.DataDirectory, id.ToString());
}