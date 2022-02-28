namespace VoidCat.Model;

public sealed record IngressPayload(Stream InStream, SecretVoidFileMeta Meta)
{
    public Guid? Id { get; init; }
    public Guid? EditSecret { get; init; }
    public string? Hash { get; init; }
    
    public bool IsAppend => Id.HasValue && EditSecret.HasValue;
}