namespace VoidCat.Model;

public static class Extensions
{
    public static Guid FromBase58Guid(this string base58)
    {
        var enc = new NBitcoin.DataEncoders.Base58Encoder();
        return new Guid(enc.DecodeData(base58));
    }

    public static string ToBase58(this Guid id)
    {
        var enc = new NBitcoin.DataEncoders.Base58Encoder();
        return enc.EncodeData(id.ToByteArray());
    }

    public static string? GetHeader(this IHeaderDictionary headers, string key)
    {
        return headers
            .FirstOrDefault(a => a.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase)).Value.ToString();
    }
}