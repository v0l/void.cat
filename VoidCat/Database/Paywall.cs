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
    public Guid FileId { get; init; }
    public File File { get; init; } = null!;
    public PaywallService Service { get; init; }
    public PaywallCurrency Currency { get; init; }
    public decimal Amount { get; init; }

    public bool Required { get; init; } = true;
    
    /// <summary>
    /// Upstream identifier, handle or lnurl
    /// </summary>
    public string? Upstream { get; init; }
}