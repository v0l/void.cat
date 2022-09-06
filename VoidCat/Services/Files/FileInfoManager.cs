using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Files;

/// <summary>
/// Main interface for getting file info to serve to clients.
/// This interface should wrap all stores and return the combined result
/// </summary>
public sealed class FileInfoManager
{
    private readonly IFileMetadataStore _metadataStore;
    private readonly IPaywallStore _paywallStore;
    private readonly IStatsReporter _statsReporter;
    private readonly IUserStore _userStore;
    private readonly IVirusScanStore _virusScanStore;
    private readonly IUserUploadsStore _userUploadsStore;

    public FileInfoManager(IFileMetadataStore metadataStore, IPaywallStore paywallStore, IStatsReporter statsReporter,
        IUserStore userStore, IVirusScanStore virusScanStore, IUserUploadsStore userUploadsStore)
    {
        _metadataStore = metadataStore;
        _paywallStore = paywallStore;
        _statsReporter = statsReporter;
        _userStore = userStore;
        _virusScanStore = virusScanStore;
        _userUploadsStore = userUploadsStore;
    }

    /// <summary>
    /// Get all metadata for a single file
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public ValueTask<PublicVoidFile?> Get(Guid id)
    {
        return Get<PublicVoidFile, VoidFileMeta>(id);
    }

    /// <summary>
    /// Get all private metadata for a single file
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public ValueTask<PrivateVoidFile?> GetPrivate(Guid id)
    {
        return Get<PrivateVoidFile, SecretVoidFileMeta>(id);
    }

    /// <summary>
    /// Get all metadata for multiple files
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Deletes all file metadata
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async ValueTask Delete(Guid id)
    {
        await _metadataStore.Delete(id);
        await _paywallStore.Delete(id);
        await _statsReporter.Delete(id);
        await _virusScanStore.Delete(id);
    }

    private async ValueTask<TFile?> Get<TFile, TMeta>(Guid id)
        where TMeta : VoidFileMeta where TFile : VoidFile<TMeta>, new()
    {
        var meta = _metadataStore.Get<TMeta>(id);
        var paywall = _paywallStore.Get(id);
        var bandwidth = _statsReporter.GetBandwidth(id);
        var virusScan = _virusScanStore.GetByFile(id);
        var uploader = _userUploadsStore.Uploader(id);
        await Task.WhenAll(meta.AsTask(), paywall.AsTask(), bandwidth.AsTask(), virusScan.AsTask(), uploader.AsTask());

        if (meta.Result == default) return default;
        var user = uploader.Result.HasValue ? await _userStore.Get<PublicVoidUser>(uploader.Result.Value) : null;

        return new TFile()
        {
            Id = id,
            Metadata = meta.Result,
            Paywall = paywall.Result,
            Bandwidth = bandwidth.Result,
            Uploader = user?.Flags.HasFlag(VoidUserFlags.PublicProfile) == true ? user : null,
            VirusScan = virusScan.Result
        };
    }
}