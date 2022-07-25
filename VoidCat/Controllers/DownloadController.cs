using System.Net;
using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Model.Paywall;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;

namespace VoidCat.Controllers;

[Route("d")]
public class DownloadController : Controller
{
    private readonly FileStoreFactory _storage;
    private readonly IFileInfoManager _fileInfo;
    private readonly IPaywallOrderStore _paywallOrders;
    private readonly ILogger<DownloadController> _logger;

    public DownloadController(FileStoreFactory storage, ILogger<DownloadController> logger, IFileInfoManager fileInfo,
        IPaywallOrderStore paywall)
    {
        _storage = storage;
        _logger = logger;
        _fileInfo = fileInfo;
        _paywallOrders = paywall;
    }

    [HttpOptions]
    [Route("{id}")]
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
    public async Task DownloadFile([FromRoute] string id)
    {
        var gid = id.FromBase58Guid();
        var voidFile = await SetupDownload(gid);
        if (voidFile == default) return;

        var egressReq = new EgressRequest(gid, GetRanges(Request, (long) voidFile!.Metadata!.Size));
        if (egressReq.Ranges.Count() > 1)
        {
            _logger.LogWarning("Multi-range request not supported!");
            // downgrade to full send
            egressReq = egressReq with
            {
                Ranges = Enumerable.Empty<RangeRequest>()
            };
        }
        else if (egressReq.Ranges.Count() == 1)
        {
            Response.StatusCode = (int) HttpStatusCode.PartialContent;
            if (egressReq.Ranges.Sum(a => a.Size) == 0)
            {
                Response.StatusCode = (int) HttpStatusCode.RequestedRangeNotSatisfiable;
                return;
            }
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

        var preResult = await _storage.StartEgress(egressReq);
        if (preResult.Redirect != null)
        {
            Response.StatusCode = (int)HttpStatusCode.Redirect;
            Response.Headers.Location = preResult.Redirect.ToString();
            Response.ContentLength = 0;
            return;
        }
        
        var cts = HttpContext.RequestAborted;
        await Response.StartAsync(cts);
        await _storage.Egress(egressReq, Response.Body, cts);
        await Response.CompleteAsync();
    }

    private async Task<PublicVoidFile?> SetupDownload(Guid id)
    {
        var meta = await _fileInfo.Get(id);
        if (meta == null)
        {
            Response.StatusCode = 404;
            return default;
        }

        // check paywall
        if (meta.Paywall != default && meta.Paywall.Service != PaymentServices.None)
        {
            var orderId = Request.Headers.GetHeader("V-OrderId") ?? Request.Query["orderId"];
            if (!await IsOrderPaid(orderId))
            {
                Response.StatusCode = (int) HttpStatusCode.PaymentRequired;
                return default;
            }
        }

        Response.Headers.XFrameOptions = "SAMEORIGIN";
        Response.Headers.ContentDisposition = $"inline; filename=\"{meta?.Metadata?.Name}\"";
        Response.ContentType = meta?.Metadata?.MimeType ?? "application/octet-stream";

        return meta;
    }

    private async ValueTask<bool> IsOrderPaid(string orderId)
    {
        if (Guid.TryParse(orderId, out var oid))
        {
            var order = await _paywallOrders.Get(oid);
            if (order?.Status == PaywallOrderStatus.Paid)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerable<RangeRequest> GetRanges(HttpRequest request, long totalSize)
    {
        foreach (var rangeHeader in request.Headers.Range)
        {
            if (string.IsNullOrEmpty(rangeHeader))
            {
                continue;
            }

            foreach (var h in RangeRequest.Parse(rangeHeader, totalSize))
            {
                yield return h;
            }
        }
    }
}