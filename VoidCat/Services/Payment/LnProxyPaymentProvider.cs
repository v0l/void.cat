using VoidCat.Database;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Payment;

public class LnProxyPaymentProvider : IPaymentProvider
{
    public ValueTask<PaywallOrder?> CreateOrder(Paywall file)
    {
        throw new NotImplementedException();
    }
    
    public ValueTask<PaywallOrder?> GetOrderStatus(Guid id)
    {
        throw new NotImplementedException();
    }
}
