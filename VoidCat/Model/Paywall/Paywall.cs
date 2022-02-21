namespace VoidCat.Model.Paywall;

public enum PaywallServices
{
    None,
    Strike
}

public abstract record PaywallConfig(PaywallServices Service, PaywallMoney Cost);

public record StrikePaywallConfig(PaywallMoney Cost) : PaywallConfig(PaywallServices.Strike, Cost)
{
    public string Handle { get; init; }
}