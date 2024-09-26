using System.Net;
using Microsoft.AspNetCore.Mvc;
using VoidCat.Database;
using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;

namespace VoidCat.Controllers;

public abstract class BaseDownloadController : Controller
{
    private readonly ILogger _logger;
    private readonly VoidSettings _settings;
    private readonly FileInfoManager _fileInfo;
    private readonly IPaymentOrderStore _paymentOrders;
    private readonly IPaymentFactory _paymentFactory;
    private readonly FileStoreFactory _storage;

    protected BaseDownloadController(VoidSettings settings, FileInfoManager fileInfo, IPaymentOrderStore paymentOrders,
        IPaymentFactory paymentFactory, ILogger logger, FileStoreFactory storage)
    {
        _settings = settings;
        _fileInfo = fileInfo;
        _paymentOrders = paymentOrders;
        _paymentFactory = paymentFactory;
        _logger = logger;
        _storage = storage;
    }

    protected async Task SendResponse(string id, VoidFileResponse voidFile)
    {
        var gid = voidFile.Id;

        if (id.EndsWith(".torrent"))
        {
            var t = await voidFile.Metadata.MakeTorrent(voidFile.Id,
                await _storage.Open(new(gid, Enumerable.Empty<RangeRequest>()), CancellationToken.None),
                _settings.SiteUrl, _settings.TorrentTrackers);

            Response.Headers.ContentDisposition = $"inline; filename=\"{id}\"";
            Response.ContentType = "application/x-bittorent";
            await t.EncodeToAsync(Response.Body);
            return;
        }

        var egressReq = new EgressRequest(gid, GetRanges(Request, (long)voidFile!.Metadata!.Size));
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
            Response.StatusCode = (int)HttpStatusCode.PartialContent;
            if (egressReq.Ranges.Sum(a => a.Size) == 0)
            {
                Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
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

    protected async Task<VoidFileResponse?> SetupDownload(Guid id)
    {
        var meta = await _fileInfo.Get(id, false);
        if (meta == null)
        {
            Response.StatusCode = 404;
            return default;
        }
        
        if (meta.Uploader?.IsNostr ?? false)
        {
            Response.StatusCode = (int)HttpStatusCode.Redirect;
            Response.Headers.Location = $"https://files.v0l.io/{meta.Metadata.Digest}";
            return default;
        }

        return await CheckDownload(meta);
    }

    private async Task<VoidFileResponse?> CheckDownload(VoidFileResponse meta)
    {
        var origin = Request.Headers.Referer.FirstOrDefault() ?? Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrEmpty(origin) && Uri.TryCreate(origin, UriKind.RelativeOrAbsolute, out var u))
        {
            if (_settings.BlockedOrigins.Any(a => string.Equals(a, u.DnsSafeHost, StringComparison.InvariantCultureIgnoreCase)))
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return default;
            }
        }

        // check payment order
        if (meta.Payment != default && meta.Payment.Service != PaywallService.None && meta.Payment.Required)
        {
            var h402 = Request.Headers.FirstOrDefault(a => a.Key.Equals("Authorization", StringComparison.InvariantCultureIgnoreCase))
                .Value.FirstOrDefault(a => a?.StartsWith("L402") ?? false);

            var orderId = Request.Headers.GetHeader("V-OrderId") ?? h402 ?? Request.Query["orderId"];
            if (!await IsOrderPaid(orderId!))
            {
                Response.Headers.CacheControl = "no-cache";
                Response.StatusCode = (int)HttpStatusCode.PaymentRequired;
                if (meta.Payment.Service is PaywallService.Strike or PaywallService.LnProxy)
                {
                    var accept = Request.Headers.GetHeader("accept");
                    if (accept == "L402")
                    {
                        var provider = await _paymentFactory.CreateProvider(meta.Payment.Service);
                        var order = await provider.CreateOrder(meta.Payment!);
                        if (order != default)
                        {
                            Response.Headers.Add("access-control-expose-headers", "www-authenticate");
                            Response.Headers.Add("www-authenticate",
                                $"L402 macaroon=\"{Convert.ToBase64String(order.Id.ToByteArray())}\", invoice=\"{order!.OrderLightning!.Invoice}\"");
                        }
                    }
                }

                return default;
            }
        }

        // prevent hot-linking viruses
        var referer = Request.Headers.Referer.Count > 0 ? new Uri(Request.Headers.Referer.First()!) : null;
        var hasCorrectReferer = referer?.Host.Equals(_settings.SiteUrl.Host, StringComparison.InvariantCultureIgnoreCase) ??
                                false;

        if (meta.VirusScan?.IsVirus == true && !hasCorrectReferer)
        {
            Response.StatusCode = (int)HttpStatusCode.Redirect;
            Response.Headers.Location = $"/{meta.Id.ToBase58()}";
            return default;
        }

        Response.Headers.XFrameOptions = "SAMEORIGIN";
        Response.Headers.ContentDisposition = $"inline; filename=\"{meta?.Metadata?.Name}\"";
        Response.ContentType = meta?.Metadata?.MimeType ?? "application/octet-stream";
        Response.ContentLength = (long?)meta?.Metadata?.Size;

        return meta;
    }

    private async ValueTask<bool> IsOrderPaid(string? orderId)
    {
        if (orderId?.StartsWith("L402") ?? false)
        {
            orderId = new Guid(Convert.FromBase64String(orderId.Substring(5).Split(":")[0])).ToString();
        }

        if (Guid.TryParse(orderId, out var oid))
        {
            var order = await _paymentOrders.Get(oid);
            if (order?.Status == PaywallOrderStatus.Paid)
            {
                return true;
            }

            if (order?.Status is PaywallOrderStatus.Unpaid)
            {
                // check status
                var svc = await _paymentFactory.CreateProvider(order.Service);
                var status = await svc.GetOrderStatus(order.Id);
                if (status != default && status.Status != order.Status)
                {
                    await _paymentOrders.UpdateStatus(order.Id, status.Status);
                }

                if (status?.Status == PaywallOrderStatus.Paid)
                {
                    return true;
                }
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
