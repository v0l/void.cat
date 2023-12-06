using ExifLibrary;
using FFMpegCore;

namespace VoidCat.Services.Files;

/// <summary>
/// Service which utilizes ffmpeg to strip metadata from media
/// and compress media to reduce storage costs
/// </summary>
public class CompressContent
{
    private readonly ILogger<CompressContent> _logger;

    public CompressContent(ILogger<CompressContent> logger)
    {
        _logger = logger;
    }

    public async Task<CompressResult> TryCompressMedia(string input, string output, CancellationToken cts)
    {
        try
        {
            string? outMime = null;
            var inExt = Path.GetExtension(input).ToLower();
            var isImage = false;
            switch (inExt)
            {
                case ".jpg":
                case ".jpeg":
                case ".gif":
                case ".png":
                case ".bmp":
                case ".tiff":
                case ".heic":
                {
                    output = Path.ChangeExtension(output, ".webp");
                    outMime = "image/webp";
                    isImage = true;
                    break;
                }
            }

            var probe = isImage ? await ImageFile.FromFileAsync(input) : default;
            var ffmpeg = FFMpegArguments
                .FromFileInput(input)
                .OutputToFile(output, true, o =>
                {
                    o.WithoutMetadata();
                    if (inExt == ".gif")
                    {
                        o.Loop(0);
                    }

                    if (probe != default)
                    {
                        var orientation = probe.Properties.Get<ExifEnumProperty<Orientation>>(ExifTag.Orientation);
                        if (orientation != default && orientation.Value != Orientation.Normal)
                        {
                            if (orientation.Value == Orientation.RotatedRight)
                            {
                                o.WithCustomArgument("-metadata:s:v rotate=\"90\"");
                            }
                            else if (orientation.Value == Orientation.RotatedLeft)
                            {
                                o.WithCustomArgument("-metadata:s:v rotate=\"-90\"");
                            }
                        }
                    }
                })
                .CancellableThrough(cts);

            _logger.LogInformation("Running: {command}", ffmpeg.Arguments);
            var result = await ffmpeg.ProcessAsynchronously();
            return new(result, output)
            {
                MimeType = outMime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not strip metadata");
        }

        return new(false, output);
    }

    public record CompressResult(bool Success, string OutPath)
    {
        public string? MimeType { get; init; }
    }
}
