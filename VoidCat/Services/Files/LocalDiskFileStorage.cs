using System.Security.Cryptography;
using VoidCat.Model;
using VoidCat.Model.Exceptions;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

/// <inheritdoc cref="IFileStore"/>
public class LocalDiskFileStore : StreamFileStore, IFileStore
{
    private readonly VoidSettings _settings;
    private readonly CompressContent _stripMetadata;

    public LocalDiskFileStore(VoidSettings settings, IAggregateStatsCollector stats, CompressContent stripMetadata)
        : base(stats)
    {
        _settings = settings;
        _stripMetadata = stripMetadata;
    }

    /// <inheritdoc />
    public async ValueTask Egress(EgressRequest request, Stream outStream, CancellationToken cts)
    {
        await using var fs = await Open(request, cts);
        await EgressFromStream(fs, request, outStream, cts);
    }

    /// <inheritdoc />
    public ValueTask<EgressResult> StartEgress(EgressRequest request)
    {
        return ValueTask.FromResult(new EgressResult());
    }

    /// <inheritdoc />
    public string Key => "local-disk";

    public ValueTask<bool> Exists(Guid id)
    {
        var path = MapPath(id);
        return ValueTask.FromResult(File.Exists(path));
    }

    public async ValueTask<Database.File> Ingress(IngressPayload payload, CancellationToken cts)
    {
        var finalPath = MapCreatePath(payload.Id);
        await using var fsTemp = new FileStream(finalPath,
            payload.IsAppend ? FileMode.Append : FileMode.Create, FileAccess.ReadWrite);

        var vf = await IngressToStream(fsTemp, payload, cts);

        if (payload.ShouldStripMetadata && payload.Segment == payload.TotalSegments)
        {
            fsTemp.Seek(0, SeekOrigin.Begin);
            var originalHash = await SHA256.Create().ComputeHashAsync(fsTemp, cts);
            fsTemp.Close();
            var ext = Path.GetExtension(vf.Name);
            var srcPath = $"{finalPath}_orig{ext}";
            File.Move(finalPath, srcPath);

            var dstPath = $"{finalPath}_dst{ext}";
            var res = await _stripMetadata.TryCompressMedia(srcPath, dstPath, cts);
            if (res.Success)
            {
                File.Move(res.OutPath, finalPath);

                File.Delete(srcPath);

                // recompute metadata
                var fInfo = new FileInfo(finalPath);
                var hash = await SHA256.Create().ComputeHashAsync(fInfo.OpenRead(), cts);
                vf = vf with
                {
                    Size = (ulong)fInfo.Length,
                    Digest = hash.ToHex(),
                    MimeType = res.MimeType ?? vf.MimeType,
                    OriginalDigest = originalHash.ToHex()
                };
            }
            else
            {
                File.Delete(srcPath);
                throw new Exception("Failed to strip metadata, please try again");
            }
        }

        if (payload.Segment == payload.TotalSegments)
        {
            var t = await vf.ToMeta(false).MakeTorrent(vf.Id,
                new FileStream(finalPath, FileMode.Open),
                _settings.SiteUrl,
                _settings.TorrentTrackers);

            var ub = new UriBuilder(_settings.SiteUrl);
            ub.Path = $"/d/{vf.Id.ToBase58()}.torrent";

            vf.MagnetLink = $"{t.GetMagnetLink()}&xs={Uri.EscapeDataString(ub.ToString())}";
        }

        return vf;
    }

    public ValueTask DeleteFile(Guid id)
    {
        var fp = MapPath(id);
        if (File.Exists(fp))
        {
            File.Delete(fp);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<Stream> Open(EgressRequest request, CancellationToken cts)
    {
        var path = MapPath(request.Id);
        if (!File.Exists(path)) throw new VoidFileNotFoundException(request.Id);

        return ValueTask.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read));
    }

    private string MapCreatePath(Guid id)
    {
        var path = MapPath(id);
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir!);
        }

        return path;
    }

    private string MapPath(Guid id) =>
        Path.Join(_settings.DataDirectory, "files-v2", id.ToString()[..2], id.ToString()[2..4], id.ToString());
}
