using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

public class FileInfoManager : IFileInfoManager
{
    private readonly IFileMetadataStore _metadataStore;
    private readonly IPaywallStore _paywallStore;
    private readonly IStatsReporter _statsReporter;
    private readonly IUserStore _userStore;
    private readonly IVirusScanStore _virusScanStore;

    public FileInfoManager(IFileMetadataStore metadataStore, IPaywallStore paywallStore, IStatsReporter statsReporter,
        IUserStore userStore, IVirusScanStore virusScanStore)
    {
        _metadataStore = metadataStore;
        _paywallStore = paywallStore;
        _statsReporter = statsReporter;
        _userStore = userStore;
        _virusScanStore = virusScanStore;
    }

    public async ValueTask<PublicVoidFile?> Get(Guid id)
    {
        var meta = _metadataStore.Get<VoidFileMeta>(id);
        var paywall = _paywallStore.Get(id);
        var bandwidth = _statsReporter.GetBandwidth(id);
        var virusScan = _virusScanStore.Get(id);
        await Task.WhenAll(meta.AsTask(), paywall.AsTask(), bandwidth.AsTask(), virusScan.AsTask());

        if (meta.Result == default) return default;

        var uploader = meta.Result?.Uploader;
        var user = uploader.HasValue ? await _userStore.Get<PublicVoidUser>(uploader.Value) : null;

        return new()
        {
            Id = id,
            Metadata = meta.Result,
            Paywall = paywall.Result,
            Bandwidth = bandwidth.Result,
            Uploader = user?.Flags.HasFlag(VoidUserFlags.PublicProfile) == true ? user : null,
            VirusScan = virusScan.Result
        };
    }

    public async ValueTask<IReadOnlyList<PublicVoidFile>> Get(Guid[] ids)
    {
        var ret = new List<PublicVoidFile>();
        foreach (var id in ids)
        {
            var v = await Get(id);
            if (v != default)
            {
                ret.Add(v);
            }
        }

        return ret;
    }

    public async ValueTask Delete(Guid id)
    {
        await _metadataStore.Delete(id);
        await _paywallStore.Delete(id);
        await _statsReporter.Delete(id);
        await _virusScanStore.Delete(id);
    }
}