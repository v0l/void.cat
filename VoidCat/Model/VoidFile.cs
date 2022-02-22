using Newtonsoft.Json;
using VoidCat.Model.Paywall;

namespace VoidCat.Model
{
    public abstract record VoidFile<TMeta> where TMeta : VoidFileMeta
    {
        /// <summary>
        /// Id of the file
        /// </summary>
        [JsonConverter(typeof(Base58GuidConverter))]
        public Guid Id { get; init; }
        
        /// <summary>
        /// Metadta related to the file
        /// </summary>
        public TMeta? Metadata { get; init; }
        
        /// <summary>
        /// Optional paywall config
        /// </summary>
        public PaywallConfig? Paywall { get; init; }
    }

    public sealed record PublicVoidFile : VoidFile<VoidFileMeta>
    {
        public Bandwidth? Bandwidth { get; init; }
    }

    public sealed record PrivateVoidFile : VoidFile<SecretVoidFileMeta>
    {
        public Bandwidth? Bandwidth { get; init; }
    }
}