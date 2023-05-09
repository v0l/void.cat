namespace VoidCat.Database;

public enum PaywallCurrency : byte
{
    BTC = 0,
    USD = 1,
    EUR = 2,
    GBP = 3
}

public enum PaywallService
{
    /// <summary>
    /// No service 
    /// </summary>
    None,

    /// <summary>
    /// Strike.me payment service
    /// </summary>
    Strike,
    
    /// <summary>
    /// LNProxy payment 
    /// </summary>
    LnProxy,
}

public class Paywall
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public File File { get; init; } = null!;
    public PaywallService Service { get; init; }
    public PaywallCurrency Currency { get; init; }
    public decimal Amount { get; init; }

    public bool Required { get; init; } = true;
    
    public PaywallStrike? PaywallStrike { get; init; }
}

public class PaywallStrike
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Paywall Paywall { get; init; } = null!;
    public string Handle { get; init; } = null!;
}