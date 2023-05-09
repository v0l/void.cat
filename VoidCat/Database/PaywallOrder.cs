namespace VoidCat.Database;

public class PaywallOrder
{
    public Guid Id { get; init; }
    public Guid FileId { get; init; }
    public File File { get; init; } = null!;
    public PaywallService Service { get; init; }
    public PaywallCurrency Currency { get; init; }
    public decimal Amount { get; init; }
    public PaywallOrderStatus Status { get; set; }
    
    public PaywallOrderLightning? OrderLightning { get; init; }
}

public enum PaywallOrderStatus : byte
{
    /// <summary>
    /// Invoice is not paid yet
    /// </summary>
    Unpaid = 0,

    /// <summary>
    /// Invoice is paid
    /// </summary>
    Paid = 1,

    /// <summary>
    /// Invoice has expired and cant be paid
    /// </summary>
    Expired = 2
}

public class PaywallOrderLightning
{
    public Guid OrderId { get; init; }
    public PaywallOrder Order { get; init; } = null!;
    public string Invoice { get; init; } = null!;
    public DateTime Expire { get; init; }
}