using Newtonsoft.Json;
using VoidCat.Model.Payments;
using VoidCat.Model.User;

namespace VoidCat.Model
{
    public abstract record VoidFile<TMeta> where TMeta : FileMeta
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
        /// Optional payment config
        /// </summary>
        public PaymentConfig? Payment { get; init; }
        
        /// <summary>
        /// User profile that uploaded the file
        /// </summary>
        public PublicUser? Uploader { get; init; }
        
        /// <summary>
        /// Traffic stats for this file
        /// </summary>
        public Bandwidth? Bandwidth { get; init; }
        
        /// <summary>
        /// Virus scanner results
        /// </summary>
        public VirusScanResult? VirusScan { get; init; }
    }

    public sealed record PublicVoidFile : VoidFile<FileMeta>
    {
    }

    public sealed record PrivateVoidFile : VoidFile<SecretFileMeta>
    {
    }
}