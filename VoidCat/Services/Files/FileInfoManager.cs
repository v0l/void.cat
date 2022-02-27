using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

public class FileInfoManager : IFileInfoManager
{
    private readonly IFileMetadataStore _metadataStore;
    private readonly IPaywallStore _paywallStore;
    private readonly IStatsReporter _statsReporter;
    private readonly IUserStore _userStore;

    public FileInfoManager(IFileMetadataStore metadataStore, IPaywallStore paywallStore, IStatsReporter statsReporter,
        IUserStore userStore)
    {
        _metadataStore = metadataStore;
        _paywallStore = paywallStore;
        _statsReporter = statsReporter;
        _userStore = userStore;
    }

    public async ValueTask<PublicVoidFile?> Get(Guid id)
    {
        var meta = _metadataStore.Get<VoidFileMeta>(id);
        var paywall = _paywallStore.GetConfig(id);
        var bandwidth = _statsReporter.GetBandwidth(id);
        await Task.WhenAll(meta.AsTask(), paywall.AsTask(), bandwidth.AsTask());

        if (meta.Result == default) return default;
        
        var uploader = meta.Result?.Uploader;
        var user = uploader.HasValue ? await _userStore.Get<PublicVoidUser>(uploader.Value) : null;

        return new()
        {
            Id = id,
            Metadata = meta.Result,
            Paywall = paywall.Result,
            Bandwidth = bandwidth.Result,
            Uploader = user?.Flags.HasFlag(VoidUserFlags.PublicProfile) == true ? user : null
        };
    }
}
