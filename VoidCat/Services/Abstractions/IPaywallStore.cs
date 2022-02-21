using VoidCat.Model.Paywall;

namespace VoidCat.Services.Abstractions;

public interface IPaywallStore
{
    ValueTask<PaywallOrder?> GetOrder(Guid id);
    ValueTask SaveOrder(PaywallOrder order);

    ValueTask<PaywallConfig?> GetConfig(Guid id);
    ValueTask SetConfig(Guid id, PaywallConfig config);
}