using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

/// <summary>
/// Primary class for accessing <see cref="IFileStore"/> implementations
/// </summary>
public class FileStoreFactory : IFileStore
{
    private readonly IFileMetadataStore _metadataStore;
    private readonly IEnumerable<IFileStore> _fileStores;

    public FileStoreFactory(IEnumerable<IFileStore> fileStores, IFileMetadataStore metadataStore)
    {
        _fileStores = fileStores;
        _metadataStore = metadataStore;
    }

    /// <summary>
    /// Get files store interface by key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public IFileStore? GetFileStore(string? key)
    {
        if (key == default && _fileStores.Count() == 1)
        {
            return _fileStores.First();
        }

        return _fileStores.FirstOrDefault(a => a.Key == key);
    }

    /// <inheritdoc />
    public string? Key => null;

    public async ValueTask<bool> Exists(Guid id)
    {
        var store = await GetStore(id);
        return await store.Exists(id);
    }

    /// <inheritdoc />
    public ValueTask<Database.File> Ingress(IngressPayload payload, CancellationToken cts)
    {
        var store = GetFileStore(payload.Meta.Storage!);
        if (store == default)
        {
            throw new InvalidOperationException($"Cannot find store '{payload.Meta.Storage}'");
        }

        return store.Ingress(payload, cts);
    }

    /// <inheritdoc />
    public async ValueTask Egress(EgressRequest request, Stream outStream, CancellationToken cts)
    {
        var store = await GetStore(request.Id);
        await store.Egress(request, outStream, cts);
    }

    /// <inheritdoc />
    public async ValueTask<EgressResult> StartEgress(EgressRequest request)
    {
        var store = await GetStore(request.Id);
        return await store.StartEgress(request);
    }

    /// <inheritdoc />
    public async ValueTask DeleteFile(Guid id)
    {
        var store = await GetStore(id);
        await store.DeleteFile(id);
    }

    /// <inheritdoc />
    public async ValueTask<Stream> Open(EgressRequest request, CancellationToken cts)
    {
        var store = await GetStore(request.Id);
        return await store.Open(request, cts);
    }

    /// <summary>
    /// Get file store for a file by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<IFileStore> GetStore(Guid id)
    {
        var meta = await _metadataStore.Get(id);
        var store = GetFileStore(meta?.Storage);

        if (store == default)
        {
            throw new InvalidOperationException($"Cannot find store '{meta?.Storage}'");
        }

        return store;
    }
}
