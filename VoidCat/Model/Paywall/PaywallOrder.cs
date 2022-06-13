namespace VoidCat.Model.Paywall;

/// <summary>
/// Status of paywall order
/// </summary>
public enum PaywallOrderStatus : byte
{
    /// <summary>
    /// Invoice is not paid yet
    /// </summary>
    Unpaid,

    /// <summary>
    /// Invoice is paid
    /// </summary>
    Paid,

    /// <summary>
    /// Invoice has expired and cant be paid
    /// </summary>
    Expired
}

/// <summary>
/// Base paywall order
/// </summary>
public class PaywallOrder
{
    /// <summary>
    /// Unique id of the order
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// File id this order is for
    /// </summary>
    public Guid File { get; init; }
    
    /// <summary>
    /// Service used to generate this order
    /// </summary>
    public PaymentServices Service { get; init; }

    /// <summary>
    /// The price of the order
    /// </summary>
    public PaywallMoney Price { get; init; } = null!;
    
    /// <summary>
    /// Current status of the order
    /// </summary>
    public PaywallOrderStatus Status { get; set; }
}

/// <summary>
/// A paywall order lightning network invoice
/// </summary>
public class LightningPaywallOrder : PaywallOrder
{
    /// <summary>
    /// Lightning invoice
    /// </summary>
    public string Invoice { get; init; } = null!;
    
    /// <summary>
    /// Expire time of the order
    /// </summary>
    public DateTime Expire { get; init; }
}