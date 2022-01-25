using VoidCat.Model;

namespace VoidCat.Services
{
    public interface IFileStorage
    {
        Task<VoidFile?> Get(Guid id);
        
        Task<InternalVoidFile> Ingress(Stream inStream, CancellationToken cts);

        Task Egress(Guid id, Stream outStream, CancellationToken cts);

        Task UpdateInfo(VoidFile patch, Guid editSecret);
    }
}
