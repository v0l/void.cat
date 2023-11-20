using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;

namespace VoidCat.Controllers;

[Route("d")]
public class DownloadController : BaseDownloadController
{
    public DownloadController(FileStoreFactory storage, ILogger<DownloadController> logger, FileInfoManager fileInfo,
        IPaymentOrderStore paymentOrderStore, VoidSettings settings, IPaymentFactory paymentFactory)
        : base(settings, fileInfo, paymentOrderStore, paymentFactory, logger, storage)
    {
    }

    [HttpOptions]
    [Route("{id}")]
    [EnableCors("*")]
    public Task DownloadFileOptions([FromRoute] string id)
    {
        var gid = id.FromBase58Guid();
        return SetupDownload(gid);
    }

    /// <summary>
    /// Download a specific file by Id
    /// </summary>
    /// <param name="id"></param>
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 86400)]
    [HttpGet]
    [Route("{id}")]
    [EnableCors("*")]
    public async Task DownloadFile([FromRoute] string id)
    {
        var gid = id.FromBase58Guid();
        var voidFile = await SetupDownload(gid);
        if (voidFile == default) return;

        await SendResponse(id, voidFile);
    }
}
