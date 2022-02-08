using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services;

namespace VoidCat.Controllers;

[Route("d")]
public class DownloadController : Controller
{
    private readonly IFileStore _storage;

    public DownloadController(IFileStore storage)
    {
        _storage = storage;
    }

    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 86400)]
    [HttpGet]
    [Route("{id}")]
    public async Task DownloadFile([FromRoute] string id)
    {
        var gid = id.FromBase58Guid();
        var meta = await _storage.Get(gid);
        if (meta == null)
        {
            Response.StatusCode = 404;
            return;
        }

        Response.Headers.XFrameOptions = "SAMEORIGIN";
        Response.Headers.ContentDisposition = $"inline; filename=\"{meta?.Metadata?.Name}\"";
        Response.ContentType = meta?.Metadata?.MimeType ?? "application/octet-stream";
        await _storage.Egress(gid, Response.Body, HttpContext.RequestAborted);
    }
}