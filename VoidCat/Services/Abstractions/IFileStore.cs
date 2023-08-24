using VoidCat.Model;
using File = VoidCat.Database.File;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// File binary data store
/// </summary>
public interface IFileStore
{
    /// <summary>
    /// Return key for named instance
    /// </summary>
    string? Key { get; }

    /// <summary>
    /// Check if a file exists in the store
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask<bool> Exists(Guid id);
    
    /// <summary>
    /// Ingress a file into the system (Upload)
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    ValueTask<File> Ingress(IngressPayload payload, CancellationToken cts);

    /// <summary>
    /// Egress a file from the system (Download)
    /// </summary>
    /// <param name="request"></param>
    /// <param name="outStream"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    ValueTask Egress(EgressRequest request, Stream outStream, CancellationToken cts);

    /// <summary>
    /// Pre-Egress checks
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    ValueTask<EgressResult> StartEgress(EgressRequest request);
    
    /// <summary>
    /// Deletes file data only, metadata must be deleted with <see cref="IFileInfoManager.Delete"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask DeleteFile(Guid id);

    /// <summary>
    /// Open a filestream for a file on the system
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    ValueTask<Stream> Open(EgressRequest request, CancellationToken cts);
}