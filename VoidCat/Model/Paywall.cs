namespace VoidCat.Model;

public record Paywall
{
    public PaywallServices Service { get; init; }
        
    public PaywallConfig? Config { get; init; }
}

public enum PaywallServices
{
    None,
    Strike
}

public enum PaywallCurrencies
{
    BTC,
    USD,
    EUR,
    GBP
}
    
public abstract record PaywallConfig
{
    public PaywallCurrencies Currency { get; init; }
    public decimal Cost { get; init; }
}
    
public record StrikePaywallConfig(string Handle) : PaywallConfig;