
using Newtonsoft.Json;

namespace VoidCat.Model
{
    public record VoidFile
    {
        [JsonConverter(typeof(Base58GuidConverter))]
        public Guid Id { get; init; }

        public VoidFileMeta? Metadata { get; set; }
        
        public ulong Size { get; init; }

        public DateTimeOffset Uploaded { get; init; }
    }

    public record InternalVoidFile : VoidFile
    {
        [JsonConverter(typeof(Base58GuidConverter))]
        public Guid EditSecret { get; init; }
    }

    public record VoidFileMeta
    {
        public string? Name { get; init; }

        public string? Description { get; init; }
        
        public string? MimeType { get; init; }
    }
}
