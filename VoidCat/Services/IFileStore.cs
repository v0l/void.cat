﻿using VoidCat.Model;

namespace VoidCat.Services
{
    public interface IFileStore
    {
        Task<VoidFile?> Get(Guid id);
        
        Task<InternalVoidFile> Ingress(Stream inStream, VoidFileMeta meta, CancellationToken cts);

        Task Egress(Guid id, Stream outStream, CancellationToken cts);

        Task UpdateInfo(VoidFile patch, Guid editSecret);

        IAsyncEnumerable<VoidFile> ListFiles();
    }
}