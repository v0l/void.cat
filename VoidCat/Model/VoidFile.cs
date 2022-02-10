
using Newtonsoft.Json;

namespace VoidCat.Model
{
    public record class VoidFile
    {
        [JsonConverter(typeof(Base58GuidConverter))]
        public Guid Id { get; init; }

        public VoidFileMeta Metadata { get; set; }
        
        public ulong Size { get; init; }

        public DateTimeOffset Uploaded { get; init; }
    }

    public record class InternalVoidFile : VoidFile
    {
        [JsonConverter(typeof(Base58GuidConverter))]
        public Guid EditSecret { get; init; }
    }

    public record class VoidFileMeta
    {
        public string? Name { get; init; }

        public string? Description { get; init; }
        
        public string? MimeType { get; init; }
    }
}
