namespace VoidCat.Model;

public sealed record IngressPayload(Stream InStream, VoidFileMeta Meta, string Hash)
{
    public Guid? Id { get; init; }
    public Guid? EditSecret { get; init; }

    public bool IsAppend => Id.HasValue && EditSecret.HasValue;
}