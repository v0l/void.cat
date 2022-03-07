using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IFileMetadataStore : IPublicPrivateStore<VoidFileMeta, SecretVoidFileMeta>
{
    ValueTask<TMeta?> Get<TMeta>(Guid id) where TMeta : VoidFileMeta;
}
