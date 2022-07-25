using VoidCat.Model;
using VoidCat.Model.Exceptions;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

/// <inheritdoc cref="IFileStore"/>
public class LocalDiskFileStore : StreamFileStore, IFileStore
{
    private const string FilesDir = "files-v1";
    private readonly ILogger<LocalDiskFileStore> _logger;
    private readonly VoidSettings _settings;

    public LocalDiskFileStore(ILogger<LocalDiskFileStore> logger, VoidSettings settings, IAggregateStatsCollector stats)
        : base(stats)
    {
        _settings = settings;
        _logger = logger;

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
    public string Key => "local-disk";
    
    /// <inheritdoc />
    public async ValueTask<PrivateVoidFile> Ingress(IngressPayload payload, CancellationToken cts)
    {
        var fPath = MapPath(payload.Id);
        await using var fsTemp = new FileStream(fPath,
            payload.IsAppend ? FileMode.Append : FileMode.Create, FileAccess.Write);
        return await IngressToStream(fsTemp, payload, cts);
    }

    /// <inheritdoc />
    public ValueTask DeleteFile(Guid id)
    {
        var fp = MapPath(id);
        if (File.Exists(fp))
        {
            _logger.LogInformation("Deleting file: {Path}", fp);
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