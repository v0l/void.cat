using VoidCat.Model;

namespace VoidCat.Services
{
    public interface IFileStore
    {
        Task<VoidFile?> Get(Guid id);

        Task<InternalVoidFile> Ingress(IngressPayload payload, CancellationToken cts);

        Task Egress(EgressRequest request, Stream outStream, CancellationToken cts);

        Task UpdateInfo(VoidFile patch, Guid editSecret);

        IAsyncEnumerable<VoidFile> ListFiles();
    }

    public record IngressPayload(Stream InStream, VoidFileMeta Meta, string Hash)
    {
        public Guid? Id { get; init; }
        public Guid? EditSecret { get; init; }

        public bool IsAppend => Id.HasValue && EditSecret.HasValue;
    }
    
    public record EgressRequest(Guid Id, IEnumerable<RangeRequest> Ranges)
    {
    }

    public record RangeRequest(long? TotalSize, long? Start, long? End)
    {
        private const long DefaultBufferSize = 1024L * 512L;
        
        public long? Size
            => Start.HasValue ?
                (End ?? Math.Min(TotalSize!.Value, Start.Value + DefaultBufferSize)) - Start.Value : End;

        public bool IsForFullFile
            => Start is 0 && !End.HasValue;

        /// <summary>
        /// Return Content-Range header content for this range
        /// </summary>
        /// <returns></returns>
        public string ToContentRange()
            => $"bytes {Start}-{End ?? (Start + Size - 1L)}/{TotalSize?.ToString() ?? "*"}";
    }
}
