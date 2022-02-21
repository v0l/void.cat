using System.Text.Json.Serialization;

namespace VoidCat.Model.Paywall;

public enum PaywallServices
{
    None,
    Strike
}

public abstract record PaywallConfig(PaywallServices Service, [property: JsonConverter(typeof(JsonStringEnumConverter))]PaywallMoney Cost);
public record StrikePaywallConfig(string Handle, PaywallMoney Cost) : PaywallConfig(PaywallServices.Strike, Cost);
