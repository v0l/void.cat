using VoidCat.Database;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// Provider to generate orders for a specific config
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Create an order with the provider
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    ValueTask<PaywallOrder?> CreateOrder(Paywall file);

    /// <summary>
    /// Get the status of an existing order with the provider
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask<PaywallOrder?> GetOrderStatus(Guid id);
}