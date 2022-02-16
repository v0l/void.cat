using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IFileStore
{
    ValueTask<VoidFile?> Get(Guid id);

    ValueTask<InternalVoidFile> Ingress(IngressPayload payload, CancellationToken cts);

    ValueTask Egress(EgressRequest request, Stream outStream, CancellationToken cts);

    ValueTask UpdateInfo(VoidFile patch, Guid editSecret);

    IAsyncEnumerable<VoidFile> ListFiles();
}

public sealed record IngressPayload(Stream InStream, VoidFileMeta Meta, string Hash)
{
    public Guid? Id { get; init; }
    public Guid? EditSecret { get; init; }

    public bool IsAppend => Id.HasValue && EditSecret.HasValue;
}

public sealed record EgressRequest(Guid Id, IEnumerable<RangeRequest> Ranges)
{
}

public sealed record RangeRequest(long? TotalSize, long? Start, long? End)
{
    private const long DefaultBufferSize = 1024L * 512L;

    public long? Size
        => Start.HasValue ? (End ?? Math.Min(TotalSize!.Value, Start.Value + DefaultBufferSize)) - Start.Value : End;

    public bool IsForFullFile
        => Start is 0 && !End.HasValue;

    /// <summary>
    /// Return Content-Range header content for this range
    /// </summary>
    /// <returns></returns>
    public string ToContentRange()
        => $"bytes {Start}-{End ?? (Start + Size - 1L)}/{TotalSize?.ToString() ?? "*"}";
}