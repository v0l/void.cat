using VoidCat.Model.Paywall;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// Paywall order store
/// </summary>
public interface IPaywallOrderStore : IBasicStore<PaywallOrder>
{
    /// <summary>
    /// Update the status of an order
    /// </summary>
    /// <param name="order"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    ValueTask UpdateStatus(Guid order, PaywallOrderStatus status);
}