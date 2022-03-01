using System.Buffers;
using System.Security.Cryptography;
using VoidCat.Model;
using VoidCat.Model.Exceptions;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

public abstract class StreamFileStore
{
    private const int BufferSize = 1_048_576;
    private readonly IAggregateStatsCollector _stats;
    private readonly IFileMetadataStore _metadataStore;
    private readonly IUserUploadsStore _userUploads;

    protected StreamFileStore(IAggregateStatsCollector stats, IFileMetadataStore metadataStore,
        IUserUploadsStore userUploads)
    {
        _stats = stats;
        _metadataStore = metadataStore;
        _userUploads = userUploads;
    }

    protected async ValueTask EgressFromStream(Stream stream, EgressRequest request, Stream outStream,
        CancellationToken cts)
    {
        if (request.Ranges.Any() && stream.CanSeek)
        {
            await EgressRanges(request.Id, request.Ranges, stream, outStream, cts);
        }
        else
        {
            await EgressFull(request.Id, stream, outStream, cts);
        }
    }

    protected async ValueTask<PrivateVoidFile> IngressToStream(Stream outStream, IngressPayload payload,
        CancellationToken cts)
    {
        var id = payload.Id;
        var meta = payload.Meta;
        if (payload.IsAppend)
        {
            if (meta?.EditSecret != null && meta.EditSecret != payload.EditSecret)
            {
                throw new VoidNotAllowedException("Edit secret incorrect!");
            }
        }

        var (total, hash) = await IngressInternal(id, payload.InStream, outStream, cts);
        if (payload.Hash != null && !hash.Equals(payload.Hash, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new CryptographicException("Invalid file hash");
        }

        return await HandleCompletedUpload(payload, hash, total);
    }

    protected async Task<PrivateVoidFile> HandleCompletedUpload(IngressPayload payload, string hash, ulong totalSize)
    {
        var meta = payload.Meta;
        if (payload.IsAppend)
        {
            meta = meta! with
            {
                Size = meta.Size + totalSize
            };
        }
        else
        {
            meta = meta! with
            {
                Digest = hash,
                Uploaded = DateTimeOffset.UtcNow,
                EditSecret = Guid.NewGuid(),
                Size = totalSize
            };
        }

        await _metadataStore.Set(payload.Id, meta);
        var vf = new PrivateVoidFile()
        {
            Id = payload.Id,
            Metadata = meta
        };

        if (meta.Uploader.HasValue)
        {
            await _userUploads.AddFile(meta.Uploader.Value, vf);
        }

        return vf;
    }
    
    private async Task<(ulong, string)> IngressInternal(Guid id, Stream ingress, Stream outStream, CancellationToken cts)
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
            await outStream.WriteAsync(buf, cts);
            await _stats.TrackIngress(id, (ulong) buf.Length);
            sha.TransformBlock(buf.ToArray(), 0, buf.Length, null, 0);
            total += (ulong) buf.Length;
            offset = 0;
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return (total, sha.Hash!.ToHex());
    }

    protected async Task EgressFull(Guid id, Stream inStream, Stream outStream,
        CancellationToken cts)
    {
        using var buffer = MemoryPool<byte>.Shared.Rent(BufferSize);
        int readLength = 0, offset = 0;
        while ((readLength = await inStream.ReadAsync(buffer.Memory[offset..], cts)) > 0 || offset != 0)
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

    private async Task EgressRanges(Guid id, IEnumerable<RangeRequest> ranges, Stream inStream, Stream outStream,
        CancellationToken cts)
    {
        using var buffer = MemoryPool<byte>.Shared.Rent(BufferSize);
        foreach (var range in ranges)
        {
            inStream.Seek(range.Start ?? range.End ?? 0L,
                range.Start.HasValue ? SeekOrigin.Begin : SeekOrigin.End);

            int readLength = 0, offset = 0;
            var dataRemaining = range.Size ?? 0L;
            while ((readLength = await inStream.ReadAsync(buffer.Memory[offset..], cts)) > 0 || offset != 0)
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
}