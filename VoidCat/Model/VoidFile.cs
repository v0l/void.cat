namespace VoidCat.Model
{
    public class VoidFile
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public string? Name { get; init; }

        public string? Description { get; init; }

        public ulong Size { get; init; }

        public DateTimeOffset Uploaded { get; init; }
    }
}
