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
}