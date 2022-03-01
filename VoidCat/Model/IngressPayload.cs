namespace VoidCat.Model;

public sealed record IngressPayload(Stream InStream, SecretVoidFileMeta Meta)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? EditSecret { get; init; }
    public string? Hash { get; init; }
    
    public bool IsAppend { get; init; }
}