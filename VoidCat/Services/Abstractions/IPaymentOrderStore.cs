using VoidCat.Model.Payments;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// Payment order store
/// </summary>
public interface IPaymentOrderStore : IBasicStore<PaymentOrder>
{
    /// <summary>
    /// Update the status of an order
    /// </summary>
    /// <param name="order"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    ValueTask UpdateStatus(Guid order, PaymentOrderStatus status);
}