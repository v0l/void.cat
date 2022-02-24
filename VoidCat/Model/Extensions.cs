using System.Security.Cryptography;
using System.Text;

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
        var h = headers
            .FirstOrDefault(a => a.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));

        return !string.IsNullOrEmpty(h.Value.ToString()) ? h.Value.ToString() : default;
    }

    public static string ToHex(this byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", string.Empty).ToLower();
    }

    private static int HexToInt(char c)
    {
        switch (c)
        {
            case '0':
                return 0;
            case '1':
                return 1;
            case '2':
                return 2;
            case '3':
                return 3;
            case '4':
                return 4;
            case '5':
                return 5;
            case '6':
                return 6;
            case '7':
                return 7;
            case '8':
                return 8;
            case '9':
                return 9;
            case 'a':
            case 'A':
                return 10;
            case 'b':
            case 'B':
                return 11;
            case 'c':
            case 'C':
                return 12;
            case 'd':
            case 'D':
                return 13;
            case 'e':
            case 'E':
                return 14;
            case 'f':
            case 'F':
                return 15;
            default:
                throw new FormatException("Unrecognized hex char " + c);
        }
    }

    private static readonly byte[,] ByteLookup = new byte[,]
    {
        // low nibble
        {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f
        },
        // high nibble
        {
            0x00, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70,
            0x80, 0x90, 0xa0, 0xb0, 0xc0, 0xd0, 0xe0, 0xf0
        }
    };

    private static byte[] FromHex(this string input)
    {
        var result = new byte[(input.Length + 1) >> 1];
        var lastCell = result.Length - 1;
        var lastChar = input.Length - 1;
        for (var i = 0; i < input.Length; i++)
        {
            result[lastCell - (i >> 1)] |= ByteLookup[i & 1, HexToInt(input[lastChar - i])];
        }

        return result;
    }

    public static string HashPassword(this string password)
    {
        return password.HashPassword("pbkdf2");
    }

    public static string HashPassword(this string password, string algo, string? saltHex = null)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        switch (algo)
        {
            case "sha256":
            {
                var hash = SHA256.Create().ComputeHash(bytes);
                return $"sha256:{hash.ToHex()}";
            }
            case "sha512":
            {
                var hash = SHA512.Create().ComputeHash(bytes);
                return $"sha512:{hash.ToHex()}";
            }
            case "pbkdf2":
            {
                const int saltSize = 32;
                const int iterations = 310_000;

                var salt = new byte[saltSize];
                if (saltHex == default)
                {
                    RandomNumberGenerator.Fill(salt);
                }
                else
                {
                    salt = saltHex.FromHex();
                }

                var pbkdf2 = new Rfc2898DeriveBytes(bytes, salt, iterations);
                return $"pbkdf2:{salt.ToHex()}:{pbkdf2.GetBytes(salt.Length).ToHex()}";
            }
        }

        throw new ArgumentException("Unknown algo", nameof(algo));
    }

    public static bool CheckPassword(this PrivateVoidUser vu, string password)
    {
        var hashParts = vu.PasswordHash.Split(":");
        return vu.PasswordHash == password.HashPassword(hashParts[0], hashParts.Length == 3 ? hashParts[1] : null);
    }
}
