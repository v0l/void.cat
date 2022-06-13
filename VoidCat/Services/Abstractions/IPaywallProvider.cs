using VoidCat.Model.Paywall;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// Provider to generate orders for a specific config
/// </summary>
public interface IPaywallProvider
{
    /// <summary>
    /// Create an order with the provider
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    ValueTask<PaywallOrder?> CreateOrder(PaywallConfig file);

    /// <summary>
    /// Get the status of an existing order with the provider
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ValueTask<PaywallOrder?> GetOrderStatus(Guid id);
}