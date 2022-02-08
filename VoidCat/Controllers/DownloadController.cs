using System.Net;
using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services;

namespace VoidCat.Controllers;

[Route("d")]
public class DownloadController : Controller
{
    private readonly IFileStore _storage;
    private readonly ILogger<DownloadController> _logger;

    public DownloadController(IFileStore storage, ILogger<DownloadController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [HttpOptions]
    [Route("{id}")]
    public Task DownloadFileOptions([FromRoute] string id)
    {
        var gid = id.FromBase58Guid();
        return SetupDownload(gid);
    }
    
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 86400)]
    [HttpGet]
    [Route("{id}")]
    public async Task DownloadFile([FromRoute] string id)
    {
        var gid = id.FromBase58Guid();
        var meta = await SetupDownload(gid);

        var egressReq = new EgressRequest(gid, GetRanges(Request, (long)meta!.Size));
        if (egressReq.Ranges.Count() > 1)
        {
            _logger.LogWarning("Multi-range request not supported!");
            // downgrade to full send
            egressReq = egressReq with
            {
                Ranges = Enumerable.Empty<RangeRequest>()
            };
        }
        else if(egressReq.Ranges.Count() == 1)
        {
            Response.StatusCode = (int)HttpStatusCode.PartialContent;
        }
        else
        {
            Response.Headers.AcceptRanges = "bytes";
        }
        
        foreach (var range in egressReq.Ranges)
        {
            Response.Headers.Add("content-range", range.ToContentRange());
            Response.ContentLength = range.Size;
        }

        var cts = HttpContext.RequestAborted;
        await Response.StartAsync(cts);
        await _storage.Egress(egressReq, Response.Body, cts);
        await Response.CompleteAsync();
    }

    private async Task<VoidFile?> SetupDownload(Guid id)
    {
        var meta = await _storage.Get(id);
        if (meta == null)
        {
            Response.StatusCode = 404;
            return null;
        }
        
        Response.Headers.XFrameOptions = "SAMEORIGIN";
        Response.Headers.ContentDisposition = $"inline; filename=\"{meta?.Metadata?.Name}\"";
        Response.ContentType = meta?.Metadata?.MimeType ?? "application/octet-stream";

        return meta;
    }

    private IEnumerable<RangeRequest> GetRanges(HttpRequest request, long totalSize)
    {
        foreach (var rangeHeader in request.Headers.Range)
        {
            if (string.IsNullOrEmpty(rangeHeader))
            {
                continue;
            }

            var ranges = rangeHeader.Replace("bytes=", string.Empty).Split(",");
            foreach (var range in ranges)
            {
                var rangeValues = range.Split("-");

                long? endByte = null, startByte = 0;
                if (long.TryParse(rangeValues[1], out var endParsed))
                    endByte = endParsed;

                if (long.TryParse(rangeValues[0], out var startParsed))
                    startByte = startParsed;

                yield return new(totalSize, startByte, endByte);
            }
        }
    }
}
