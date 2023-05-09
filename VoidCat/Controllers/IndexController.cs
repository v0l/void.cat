using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers;

public class IndexController : Controller
{
    private readonly IWebHostEnvironment _webHost;
    private readonly IFileMetadataStore _fileMetadata;

    public IndexController(IFileMetadataStore fileMetadata, IWebHostEnvironment webHost)
    {
        _fileMetadata = fileMetadata;
        _webHost = webHost;
    }

    /// <summary>
    /// Return html content with tags for file preview
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Route("{id}")]
    [HttpGet]
    public async Task<IActionResult> FilePreview(string id)
    {
        id.TryFromBase58Guid(out var gid);

        var manifestPath = Path.Combine(_webHost.WebRootPath, "asset-manifest.json");
        if (!System.IO.File.Exists(manifestPath)) return StatusCode(500);

        // old format hash, return 404
        if (id.Length == 40 && Regex.IsMatch(id, @"[0-9a-z]{40}"))
        {
            Response.StatusCode = 404;
        }
        
        var jsonManifest = await System.IO.File.ReadAllTextAsync(manifestPath);
        return View("~/Pages/Index.cshtml", new IndexModel
        {
            Id = gid,
            Meta = (await _fileMetadata.Get(gid))?.ToMeta(false),
            Manifest = JsonConvert.DeserializeObject<AssetManifest>(jsonManifest)!
        });
    }

    public class IndexModel
    {
        public Guid Id { get; init; }
        public VoidFileMeta? Meta { get; init; }

        public AssetManifest Manifest { get; init; }
    }

    public class AssetManifest
    {
        public Dictionary<string, string> Files { get; init; }
        public List<string> Entrypoints { get; init; }
    }
}