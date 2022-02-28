using System.Buffers;
using System.Security.Cryptography;
using VoidCat.Model;
using VoidCat.Model.Exceptions;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

public class LocalDiskFileStore : IFileStore
{
    private const int BufferSize = 1_048_576;
    private readonly ILogger<LocalDiskFileStore> _logger;
    private readonly VoidSettings _settings;
    private readonly IAggregateStatsCollector _stats;
    private readonly IFileMetadataStore _metadataStore;
    private readonly IFileInfoManager _fileInfo;
    private readonly IUserUploadsStore _userUploads;

    public LocalDiskFileStore(ILogger<LocalDiskFileStore> logger, VoidSettings settings, IAggregateStatsCollector stats,
        IFileMetadataStore metadataStore, IFileInfoManager fileInfo, IUserUploadsStore userUploads)
    {
        _settings = settings;
        _stats = stats;
        _metadataStore = metadataStore;
        _fileInfo = fileInfo;
        _userUploads = userUploads;
        _logger = logger;

        if (!Directory.Exists(_settings.DataDirectory))
        {
            Directory.CreateDirectory(_settings.DataDirectory);
        }
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
        var meta = payload.Meta;
        if (payload.IsAppend)
        {
            if (meta?.EditSecret != null && meta.EditSecret != payload.EditSecret)
            {
                throw new VoidNotAllowedException("Edit secret incorrect!");
            }
        }

        // open file
        await using var fsTemp = new FileStream(fPath,
            payload.IsAppend ? FileMode.Append : FileMode.Create, FileAccess.Write);

        var (total, hash) = await IngressInternal(id, payload.InStream, fsTemp, cts);

        if (payload.Hash != null && !hash.Equals(payload.Hash, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new CryptographicException("Invalid file hash");
        }

        if (payload.IsAppend)
        {
            meta = meta! with
            {
                Size = meta.Size + total
            };
        }
        else
        {
            meta = meta! with
            {
                Digest = hash,
                Uploaded = DateTimeOffset.UtcNow,
                EditSecret = Guid.NewGuid(),
                Size = total
            };
        }

        await _metadataStore.Set(id, meta);
        var vf = new PrivateVoidFile()
        {
            Id = id,
            Metadata = meta
        };

        if (meta.Uploader.HasValue)
        {
            await _userUploads.AddFile(meta.Uploader.Value, vf);
        }

        return vf;
    }

    public ValueTask<PagedResult<PublicVoidFile>> ListFiles(PagedRequest request)
    {
        var files = Directory.EnumerateFiles(_settings.DataDirectory)
            .Where(a => !Path.HasExtension(a));

        files = (request.SortBy, request.SortOrder) switch
        {
            (PagedSortBy.Id, PageSortOrder.Asc) => files.OrderBy(a =>
                Guid.TryParse(Path.GetFileNameWithoutExtension(a), out var g) ? g : Guid.Empty),
            (PagedSortBy.Id, PageSortOrder.Dsc) => files.OrderByDescending(a =>
                Guid.TryParse(Path.GetFileNameWithoutExtension(a), out var g) ? g : Guid.Empty),
            (PagedSortBy.Name, PageSortOrder.Asc) => files.OrderBy(Path.GetFileNameWithoutExtension),
            (PagedSortBy.Name, PageSortOrder.Dsc) => files.OrderByDescending(Path.GetFileNameWithoutExtension),
            (PagedSortBy.Size, PageSortOrder.Asc) => files.OrderBy(a => new FileInfo(a).Length),
            (PagedSortBy.Size, PageSortOrder.Dsc) => files.OrderByDescending(a => new FileInfo(a).Length),
            (PagedSortBy.Date, PageSortOrder.Asc) => files.OrderBy(File.GetCreationTimeUtc),
            (PagedSortBy.Date, PageSortOrder.Dsc) => files.OrderByDescending(File.GetCreationTimeUtc),
            _ => files
        };

        async IAsyncEnumerable<PublicVoidFile> EnumeratePage(IEnumerable<string> page)
        {
            foreach (var file in page)
            {
                if (!Guid.TryParse(Path.GetFileNameWithoutExtension(file), out var gid)) continue;

                var loaded = await _fileInfo.Get(gid);
                if (loaded != default)
                {
                    yield return loaded;
                }
            }
        }

        return ValueTask.FromResult(new PagedResult<PublicVoidFile>()
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalResults = files.Count(),
            Results = EnumeratePage(files.Skip(request.PageSize * request.Page).Take(request.PageSize))
        });
    }

    public async ValueTask DeleteFile(Guid id)
    {
        var fp = MapPath(id);
        if (File.Exists(fp))
        {
            _logger.LogInformation("Deleting file: {Path}", fp);
            File.Delete(fp);
        }

        await _metadataStore.Delete(id);
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
            await _stats.TrackIngress(id, (ulong)buf.Length);
            sha.TransformBlock(buf.ToArray(), 0, buf.Length, null, 0);
            total += (ulong)buf.Length;
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
            await _stats.TrackEgress(id, (ulong)fullSize);
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
                await outStream.WriteAsync(buffer.Memory[..(int)toWrite], cts);
                await _stats.TrackEgress(id, (ulong)toWrite);
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
