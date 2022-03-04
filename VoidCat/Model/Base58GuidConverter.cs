using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace VoidCat.Model;

public class Base58GuidConverter : JsonConverter<Guid>
{
    public override void WriteJson(JsonWriter writer, Guid value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToBase58());
    }

    public override Guid ReadJson(JsonReader reader, Type objectType, Guid existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String && existingValue == Guid.Empty)
        {
            var str = reader.Value as string;
            if ((str?.Contains('-') ?? false) && Guid.TryParse(str, out var g))
            {
                return g;
            }
            return str?.FromBase58Guid() ?? existingValue;
        }

        return existingValue;
    }
}