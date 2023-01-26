using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using Newtonsoft.Json;

namespace VoidCat.Services.Files;

/// <summary>
/// Service which utilizes ffmpeg to strip metadata from media
/// </summary>
public class StripMetadata
{
    private readonly ILogger<StripMetadata> _logger;

    public StripMetadata(ILogger<StripMetadata> logger)
    {
        _logger = logger;
    }

    public async Task<bool> TryStripMediaMetadata(string input, string output, CancellationToken cts)
    {
        try
        {
            var ffprobe = await FFProbe.AnalyseAsync(input, cancellationToken: cts);
            if (ffprobe == default)
            {
                throw new InvalidOperationException("Could not determine media type with ffprobe");
            }

            _logger.LogInformation("Stripping content from {type}", ffprobe.Format.FormatName);

            var ffmpeg = FFMpegArguments
                .FromFileInput(input)
                .OutputToFile(output, true, o =>
                {
                    o.WithoutMetadata();
                })
                .CancellableThrough(cts);

            _logger.LogInformation("Running: {command}", ffmpeg.Arguments);
            return await ffmpeg.ProcessAsynchronously();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not strip metadata");
        }

        return false;
    }
}
