using VoidCat.Model.Payments;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// Factory class to access service provider implementations
/// </summary>
public interface IPaymentFactory
{
    /// <summary>
    /// Create provider handler for specified service type
    /// </summary>
    /// <param name="svc"></param>
    /// <returns></returns>
    ValueTask<IPaymentProvider> CreateProvider(PaymentServices svc);
}