using System.Collections.Immutable;
using VoidCat.Database;
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
    private readonly IPaymentStore _paymentStore;
    private readonly IStatsReporter _statsReporter;
    private readonly IUserStore _userStore;
    private readonly IVirusScanStore _virusScanStore;
    private readonly IUserUploadsStore _userUploadsStore;

    public FileInfoManager(IFileMetadataStore metadataStore, IPaymentStore paymentStore, IStatsReporter statsReporter,
        IUserStore userStore, IVirusScanStore virusScanStore, IUserUploadsStore userUploadsStore)
    {
        _metadataStore = metadataStore;
        _paymentStore = paymentStore;
        _statsReporter = statsReporter;
        _userStore = userStore;
        _virusScanStore = virusScanStore;
        _userUploadsStore = userUploadsStore;
    }

    /// <summary>
    /// Get all metadata for a single file
    /// </summary>
    /// <param name="id"></param>
    /// <param name="withEditSecret"></param>
    /// <returns></returns>
    public async ValueTask<VoidFileResponse?> Get(Guid id, bool withEditSecret)
    {
        var meta = await _metadataStore.Get(id);
        if (meta == default) return default;

        var bandwidth = await _statsReporter.GetBandwidth(id);
        var virusScan = await _virusScanStore.GetByFile(id);
        var uploader = await _userUploadsStore.Uploader(id);

        var user = uploader.HasValue ? await _userStore.Get(uploader.Value) : null;

        return new VoidFileResponse
        {
            Id = id,
            Metadata = meta.ToMeta(withEditSecret),
            Payment = meta.Paywall,
            Bandwidth = bandwidth,
            Uploader = user?.Flags.HasFlag(UserFlags.PublicProfile) == true || withEditSecret ? user?.ToApiUser(false) : null,
            VirusScan = virusScan?.ToVirusStatus()
        };
    }

    /// <summary>
    /// Get all metadata for multiple files
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="withEditSecret"></param>
    /// <returns></returns>
    public async ValueTask<IReadOnlyList<VoidFileResponse>> Get(Guid[] ids, bool withEditSecret)
    {
        //todo: improve this
        var ret = new List<VoidFileResponse>();
        foreach (var i in ids)
        {
            var x = await Get(i, withEditSecret);
            if (x != default)
            {
                ret.Add(x);
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
        await _paymentStore.Delete(id);
        await _statsReporter.Delete(id);
        await _virusScanStore.Delete(id);
    }
}
