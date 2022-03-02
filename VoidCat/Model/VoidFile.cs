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
        
        /// <summary>
        /// User profile that uploaded the file
        /// </summary>
        public PublicVoidUser? Uploader { get; init; }
        
        /// <summary>
        /// Traffic stats for this file
        /// </summary>
        public Bandwidth? Bandwidth { get; init; }
    }

    public sealed record PublicVoidFile : VoidFile<VoidFileMeta>
    {
    }

    public sealed record PrivateVoidFile : VoidFile<SecretVoidFileMeta>
    {
    }
}