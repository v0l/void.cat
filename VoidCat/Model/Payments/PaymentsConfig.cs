namespace VoidCat.Model.Payments;

/// <summary>
/// Base payment config
/// </summary>
public abstract class PaymentConfig
{
    /// <summary>
    /// File this config is for
    /// </summary>
    public Guid File { get; init; }
    
    /// <summary>
    /// Service used to pay the payment
    /// </summary>
    public PaymentServices Service { get; init; } = PaymentServices.None;

    /// <summary>
    /// The cost for the payment to pass
    /// </summary>
    public PaymentMoney Cost { get; init; } = new(0m, PaymentCurrencies.BTC);

    /// <summary>
    /// If the payment is required
    /// </summary>
    public bool Required { get; init; } = true;
}

/// <inheritdoc />
public sealed class NoPaymentConfig : PaymentConfig
{
    
}

/// <summary>
/// Payment config for <see cref="PaymentServices.Strike"/> service
/// </summary>
/// <param name="Cost"></param>
public sealed class StrikePaymentConfig : PaymentConfig
{
    /// <summary>
    /// Strike username to pay to
    /// </summary>
    public string Handle { get; init; } = null!;
}