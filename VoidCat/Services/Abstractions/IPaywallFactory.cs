using VoidCat.Model;
using VoidCat.Model.Paywall;

namespace VoidCat.Services.Abstractions;

public interface IPaywallFactory
{
    ValueTask<IPaywallProvider> CreateProvider(PaywallServices svc);
}

public interface IPaywallProvider
{
    ValueTask<PaywallOrder?> CreateOrder(PublicVoidFile file);

    ValueTask<PaywallOrder?> GetOrderStatus(Guid id);
}

public interface IPaywallStore
{
    ValueTask<PaywallOrder?> GetOrder(Guid id);
    ValueTask SaveOrder(PaywallOrder order);

    ValueTask<PaywallConfig?> GetConfig(Guid id);
    ValueTask SetConfig(Guid id, PaywallConfig config);
}
