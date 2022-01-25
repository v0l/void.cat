using VoidCat.Model;

namespace VoidCat.Services
{
    public interface IFileIngressFactory
    {
        Task<VoidFile> Ingress(Stream inStream);
    }
}
