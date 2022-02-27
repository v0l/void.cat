using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IFileStore
{
    ValueTask<PrivateVoidFile> Ingress(IngressPayload payload, CancellationToken cts);

    ValueTask Egress(EgressRequest request, Stream outStream, CancellationToken cts);

    ValueTask<PagedResult<PublicVoidFile>> ListFiles(PagedRequest request);

    ValueTask DeleteFile(Guid id);
}