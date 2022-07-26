namespace VoidCat.Model;

public sealed record IngressPayload(Stream InStream, SecretVoidFileMeta Meta, int Segment, int TotalSegments)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? EditSecret { get; init; }

    public bool IsAppend => Segment > 1 && IsMultipart;

    public bool IsMultipart => TotalSegments > 1;
}