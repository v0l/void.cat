using System.Text.Json.Serialization;

namespace VoidCat.Model.Payments;

/// <summary>
/// Money amount for payment orders
/// </summary>
/// <param name="Amount"></param>
/// <param name="Currency"></param>
public record PaymentMoney(decimal Amount, PaymentCurrencies Currency);

/// <summary>
/// Supported payment currencies
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentCurrencies : byte
{
    BTC = 0,
    USD = 1,
    EUR = 2,
    GBP = 3
}
