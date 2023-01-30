using System.Security.Cryptography;
using VoidCat.Model;
using VoidCat.Model.Exceptions;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

/// <inheritdoc cref="IFileStore"/>
public class LocalDiskFileStore : StreamFileStore, IFileStore
{
    private const string FilesDir = "files-v1";
    private readonly VoidSettings _settings;
    private readonly CompressContent _stripMetadata;

    public LocalDiskFileStore(VoidSettings settings, IAggregateStatsCollector stats, CompressContent stripMetadata)
        : base(stats)
    {
        _settings = settings;
        _stripMetadata = stripMetadata;

        var dir = Path.Combine(_settings.DataDirectory, FilesDir);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
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

    /// <inheritdoc />
    public async ValueTask<PrivateVoidFile> Ingress(IngressPayload payload, CancellationToken cts)
    {
        var finalPath = MapPath(payload.Id);
        await using var fsTemp = new FileStream(finalPath,
            payload.IsAppend ? FileMode.Append : FileMode.Create, FileAccess.ReadWrite);

        var vf = await IngressToStream(fsTemp, payload, cts);
        
        if (payload.ShouldStripMetadata && payload.Segment == payload.TotalSegments)
        {
            fsTemp.Close();
            var ext = Path.GetExtension(vf.Metadata!.Name);
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
                    Metadata = vf.Metadata! with
                    {
                        Size = (ulong)fInfo.Length,
                        Digest = hash.ToHex(),
                        MimeType = res.MimeType ?? vf.Metadata.MimeType
                    }
                };
            }
            else
            {
                // move orig file back
                File.Move(srcPath, finalPath);
            }
        }

        return vf;
    }

    /// <inheritdoc />
    public ValueTask DeleteFile(Guid id)
    {
        var fp = MapPath(id);
        if (File.Exists(fp))
        {
            File.Delete(fp);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<Stream> Open(EgressRequest request, CancellationToken cts)
    {
        var path = MapPath(request.Id);
        if (!File.Exists(path)) throw new VoidFileNotFoundException(request.Id);

        return ValueTask.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read));
    }

    private string MapPath(Guid id) =>
        Path.Join(_settings.DataDirectory, FilesDir, id.ToString());
}
