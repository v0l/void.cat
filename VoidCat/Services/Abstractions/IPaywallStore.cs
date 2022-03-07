using VoidCat.Model.Paywall;

namespace VoidCat.Services.Abstractions;

public interface IPaywallStore : IBasicStore<PaywallConfig>
{
    ValueTask<PaywallOrder?> GetOrder(Guid id);
    ValueTask SaveOrder(PaywallOrder order);
}