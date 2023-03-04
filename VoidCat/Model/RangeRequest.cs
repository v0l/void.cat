namespace VoidCat.Model;

public sealed record RangeRequest(long? TotalSize, long? Start, long? End)
{
    private const long DefaultBufferSize = 1024L * 512L;

    public string OriginalString { get; private init; }
    
    public long? Size
        => (Start.HasValue ? (End ?? Math.Min(TotalSize!.Value, Start.Value + DefaultBufferSize)) - Start.Value : End) + 1L;

    /// <summary>
    /// Return Content-Range header content for this range
    /// </summary>
    /// <returns></returns>
    public string ToContentRange()
        => $"bytes {Start}-{End ?? (Start + Size - 1L)}/{TotalSize?.ToString() ?? "*"}";

    public static IEnumerable<RangeRequest> Parse(string header, long totalSize)
    {
        var ranges = header.Replace("bytes=", string.Empty).Split(",");
        foreach (var range in ranges)
        {
            var rangeValues = range.Split("-");

            long? endByte = null, startByte = 0;
            if (long.TryParse(rangeValues[1], out var endParsed))
                endByte = endParsed;

            if (long.TryParse(rangeValues[0], out var startParsed))
                startByte = startParsed;

            yield return new(totalSize, startByte, endByte)
            {
                OriginalString = range
            };
        }
    }
}