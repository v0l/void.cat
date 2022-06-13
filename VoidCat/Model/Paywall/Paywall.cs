namespace VoidCat.Model.Paywall;

/// <summary>
/// Payment services supported by the system
/// </summary>
public enum PaymentServices
{
    /// <summary>
    /// No service 
    /// </summary>
    None,

    /// <summary>
    /// Strike.me payment service
    /// </summary>
    Strike
}

/// <summary>
/// Base paywall config
/// </summary>
public abstract class PaywallConfig
{
    /// <summary>
    /// File this config is for
    /// </summary>
    public Guid File { get; init; }
    
    /// <summary>
    /// Service used to pay the paywall
    /// </summary>
    public PaymentServices Service { get; init; } = PaymentServices.None;

    /// <summary>
    /// The cost for the paywall to pass
    /// </summary>
    public PaywallMoney Cost { get; init; } = new(0m, PaywallCurrencies.BTC);
}

/// <inheritdoc />
public sealed class NoPaywallConfig : PaywallConfig
{
    
}

/// <summary>
/// Paywall config for <see cref="PaymentServices.Strike"/> service
/// </summary>
/// <param name="Cost"></param>
public sealed class StrikePaywallConfig : PaywallConfig
{
    /// <summary>
    /// Strike username to pay to
    /// </summary>
    public string Handle { get; init; } = null!;
}