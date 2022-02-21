using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IFileStore
{
    ValueTask<PublicVoidFile?> Get(Guid id);

    ValueTask<PrivateVoidFile> Ingress(IngressPayload payload, CancellationToken cts);

    ValueTask Egress(EgressRequest request, Stream outStream, CancellationToken cts);

    IAsyncEnumerable<PublicVoidFile> ListFiles();
}