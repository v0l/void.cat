using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IFileMetadataStore : IPublicPrivateStore<VoidFileMeta, SecretVoidFileMeta>
{
    ValueTask<TMeta?> Get<TMeta>(Guid id) where TMeta : VoidFileMeta;
    ValueTask<IReadOnlyList<TMeta>> Get<TMeta>(Guid[] ids) where TMeta : VoidFileMeta;
    ValueTask Update<TMeta>(Guid id, TMeta meta) where TMeta : VoidFileMeta;

    /// <summary>
    /// Returns basic stats about the file store
    /// </summary>
    /// <returns></returns>
    ValueTask<StoreStats> Stats();

    public sealed record StoreStats(long Files, ulong Size);
}