namespace VoidCat.Services.VirusScanner.Exceptions;

public class RateLimitedException : Exception
{
    public DateTimeOffset? RetryAfter { get; init; }
}