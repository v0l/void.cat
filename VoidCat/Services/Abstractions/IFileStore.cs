using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IFileStore
{
    ValueTask<PrivateVoidFile> Ingress(IngressPayload payload, CancellationToken cts);

    ValueTask Egress(EgressRequest request, Stream outStream, CancellationToken cts);

    ValueTask<PagedResult<PublicVoidFile>> ListFiles(PagedRequest request);

    /// <summary>
    /// Deletes file data only, metadata must be deleted with <see cref="IFileInfoManager.Delete"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask DeleteFile(Guid id);

    ValueTask<Stream> Open(EgressRequest request, CancellationToken cts);
}