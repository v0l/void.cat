using System.Text.Json.Serialization;

namespace VoidCat.Model.Paywall;

public record PaywallMoney(decimal Amount, PaywallCurrencies Currency);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaywallCurrencies : byte
{
    BTC = 0,
    USD = 1,
    EUR = 2,
    GBP = 3
}
