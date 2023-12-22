using AngleSharp;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers;

public class IndexController : Controller
{
    private readonly IWebHostEnvironment _webHost;
    private readonly IFileMetadataStore _fileMetadata;
    private readonly VoidSettings _settings;

    public IndexController(IFileMetadataStore fileMetadata, IWebHostEnvironment webHost, VoidSettings settings)
    {
        _fileMetadata = fileMetadata;
        _webHost = webHost;
        _settings = settings;
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

        var ubDownload = new UriBuilder(_settings.SiteUrl)
        {
            Path = $"/d/{gid.ToBase58()}"
        };

        var ubView = new UriBuilder(_settings.SiteUrl)
        {
            Path = $"/{gid.ToBase58()}"
        };

        var indexPath = Path.Combine(_webHost.WebRootPath, "index.html");
        var indexContent = await System.IO.File.ReadAllTextAsync(indexPath);

        var meta = (await _fileMetadata.Get(gid))?.ToMeta(false);
        var tags = new List<KeyValuePair<string, string>>()
        {
            new("site_name", "void.cat"),
            new("title", meta?.Name ?? ""),
            new("description", meta?.Description ?? ""),
            new("url", ubView.Uri.ToString()),
        };

        var mime = meta?.MimeType;
        if (mime?.StartsWith("image/") ?? false)
        {
            tags.Add(new("type", "image"));
            tags.Add(new("image", ubDownload.Uri.ToString()));
            tags.Add(new("image:type", mime));
        }
        else if (mime?.StartsWith("video/") ?? false)
        {
            tags.Add(new("type", "video.other"));
            tags.Add(new("image", ""));
            tags.Add(new("video", ubDownload.Uri.ToString()));
            tags.Add(new("video:url", ubDownload.Uri.ToString()));
            tags.Add(new("video:secure_url", ubDownload.Uri.ToString()));
            tags.Add(new("video:type", mime));
        }
        else if (mime?.StartsWith("audio/") ?? false)
        {
            tags.Add(new("type", "audio.other"));
            tags.Add(new("audio", ubDownload.Uri.ToString()));
            tags.Add(new("audio:type", mime));
        }
        else
        {
            tags.Add(new("type", "website"));
        }

        var injectedHtml = await InjectTags(indexContent, tags);
        return Content(injectedHtml?.ToHtml() ?? indexContent, "text/html");
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

    private async Task<IDocument?> InjectTags(string html, List<KeyValuePair<string, string>> tags)
    {
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var doc = await context.OpenAsync(c => c.Content(html));

        foreach (var tag in tags)
        {
            var ogTag = doc.CreateElement("meta");
            ogTag.SetAttribute("property", $"og:{tag.Key}");
            ogTag.SetAttribute("content", tag.Value);
            doc.Head?.AppendChild(ogTag);
            switch (tag.Key.ToLower())
            {
                case "title":
                {
                    var titleTag = doc.Head?.QuerySelector("title");
                    if (titleTag != default)
                    {
                        titleTag.TextContent = tag.Value;
                    }

                    break;
                }
                case "description":
                {
                    var descriptionTag = doc.Head?.QuerySelector("meta[name='description']");
                    descriptionTag?.SetAttribute("content", tag.Value);

                    break;
                }
            }
        }

        return doc;
    }
}
