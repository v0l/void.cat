namespace VoidCat.Model
{
    public class VoidSettings
    {
        public string DataDirectory { get; init; } = "./data";
        public Uri? SeqHost { get; init; }
        public string? SeqApiKey { get; init; }
    }
}
