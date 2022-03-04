using VoidCat.Model.Paywall;

namespace VoidCat.Services.Abstractions;

public interface IPaywallStore
{
    ValueTask<PaywallOrder?> GetOrder(Guid id);
    ValueTask SaveOrder(PaywallOrder order);

    ValueTask<PaywallConfig?> Get(Guid id);
    ValueTask Set(Guid id, PaywallConfig config);
    ValueTask Delete(Guid id);
}