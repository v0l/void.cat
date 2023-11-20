using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Services;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;
using VoidCat.Services.Users;
using File = VoidCat.Database.File;

namespace VoidCat.Controllers;

[Route("nostr")]
public class NostrController : BaseDownloadController
{
    private readonly VoidSettings _settings;
    private readonly UserManager _userManager;
    private readonly FileStoreFactory _storeFactory;
    private readonly IFileMetadataStore _fileMetadata;
    private readonly IUserUploadsStore _userUploads;
    private readonly FileInfoManager _fileInfo;

    public NostrController(VoidSettings settings, UserManager userManager, FileStoreFactory storeFactory, IFileMetadataStore fileMetadata,
        IUserUploadsStore userUploads, FileInfoManager fileInfo, IPaymentOrderStore paymentOrderStore, IPaymentFactory paymentFactory,
        ILogger<NostrController> logger)
        : base(settings, fileInfo, paymentOrderStore, paymentFactory, logger, storeFactory)
    {
        _settings = settings;
        _userManager = userManager;
        _storeFactory = storeFactory;
        _fileMetadata = fileMetadata;
        _userUploads = userUploads;
        _fileInfo = fileInfo;
    }

    [HttpGet("/.well-known/nostr/nip96.json")]
    public IActionResult GetInfo()
    {
        var info = new Nip96Info
        {
            ApiUri = new Uri(_settings.SiteUrl, "/nostr"),
            Plans = new()
            {
                {
                    "free", new Nip96Plan
                    {
                        Name = "Default",
                        MaxUploadSize = (long?)_settings.UploadSegmentSize
                    }
                }
            }
        };

        return Json(info);
    }

    [HttpPost]
    [DisableRequestSizeLimit]
    [DisableFormValueModelBinding]
    [Authorize(AuthenticationSchemes = NostrAuth.Scheme, Policy = Policies.RequireNostr)]
    public async Task<IActionResult> Upload()
    {
        var pubkey = HttpContext.GetPubKey();
        if (string.IsNullOrEmpty(pubkey))
        {
            return Unauthorized();
        }

        try
        {
            var nostrUser = await _userManager.LoginOrRegister(pubkey);
            var file = Request.Form.Files.First();

            var meta = new File
            {
                MimeType = file.ContentType,
                Name = file.FileName,
                Description = Request.Form.TryGetValue("alt", out var sd) ? sd.First() : default,
                Size = (ulong)file.Length,
                Storage = nostrUser.Storage
            };

            var vf = await _storeFactory.Ingress(new(file.OpenReadStream(), meta, 1, 1, true), HttpContext.RequestAborted);

            // save metadata
            await _fileMetadata.Add(vf);
            await _userUploads.AddFile(nostrUser.Id, vf.Id);

            var ret = new Nip96UploadResult
            {
                FileHeader = new()
                {
                    Tags = new()
                    {
                        new() {"url", new Uri(_settings.SiteUrl, $"/nostr/{vf.OriginalDigest}{Path.GetExtension(vf.Name)}").ToString()},
                        new() {"ox", vf.OriginalDigest ?? "", _settings.SiteUrl.ToString()},
                        new() {"x", vf.Digest ?? ""},
                        new() {"m", vf.MimeType}
                    }
                }
            };

            return Json(ret);
        }
        catch (Exception ex)
        {
            return Json(new Nip96UploadResult()
            {
                Status = "error",
                Message = ex.Message
            });
        }
    }

    [HttpGet("{id}")]
    public async Task GetFile([FromRoute] string id)
    {
        var digest = Path.GetFileNameWithoutExtension(id);
        var file = await _fileMetadata.GetHash(digest);
        if (file == default)
        {
            Response.StatusCode = 404;
            return;
        }

        var meta = await SetupDownload(file.Id);
        if (meta == default) return;

        await SendResponse(id, meta);
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = NostrAuth.Scheme, Policy = Policies.RequireNostr)]
    public async Task<IActionResult> DeleteFile([FromRoute] string id)
    {
        var digest = Path.GetFileNameWithoutExtension(id);
        var file = await _fileMetadata.GetHash(digest);
        if (file == default)
        {
            return NotFound();
        }

        var pubkey = HttpContext.GetPubKey();
        if (string.IsNullOrEmpty(pubkey))
        {
            return Unauthorized();
        }

        var nostrUser = await _userManager.LoginOrRegister(pubkey);
        var uploader = await _userUploads.Uploader(file.Id);
        if (uploader == default || uploader != nostrUser.Id)
        {
            return Forbid();
        }

        await _fileInfo.Delete(file.Id);

        return Json(new Nip96UploadResult());
    }
}

public class Nip96Info
{
    [JsonProperty("api_url")]
    public Uri ApiUri { get; init; } = null!;

    [JsonProperty("download_url")]
    public Uri? DownloadUrl { get; init; }

    [JsonProperty("delegated_to_url")]
    public Uri? DelegatedTo { get; init; }

    [JsonProperty("supported_nips")]
    public List<int>? SupportedNips { get; init; }

    [JsonProperty("tos_url")]
    public Uri? Tos { get; init; }

    [JsonProperty("content_types")]
    public List<string>? ContentTypes { get; init; }

    [JsonProperty("plans")]
    public Dictionary<string, Nip96Plan>? Plans { get; init; }
}

public class Nip96Plan
{
    [JsonProperty("name")]
    public string Name { get; init; } = null!;

    [JsonProperty("is_nip98_required")]
    public bool Nip98Required { get; init; } = true;

    [JsonProperty("url")]
    public Uri? LandingPage { get; init; }

    [JsonProperty("max_byte_size")]
    public long? MaxUploadSize { get; init; }

    [JsonProperty("file_expiration")]
    public int[] FileExpiration { get; init; } = {0, 0};

    [JsonProperty("media_transformations")]
    public Nip96MediaTransformations? MediaTransformations { get; init; }
}

public class Nip96MediaTransformations
{
    [JsonProperty("image")]
    public List<string>? Image { get; init; }
}

public class Nip96UploadResult
{
    [JsonProperty("status")]
    public string Status { get; init; } = "success";

    [JsonProperty("message")]
    public string? Message { get; init; }

    [JsonProperty("processing_url")]
    public Uri? ProcessingUrl { get; init; }

    [JsonProperty("nip94_event")]
    public Nip94Info FileHeader { get; init; } = null!;
}

public class Nip94Info
{
    [JsonProperty("tags")]
    public List<List<string>> Tags { get; init; } = new();
}
