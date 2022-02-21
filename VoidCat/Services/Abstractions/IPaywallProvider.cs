using VoidCat.Model;
using VoidCat.Model.Paywall;

namespace VoidCat.Services.Abstractions;

public interface IPaywallProvider
{
    ValueTask<PaywallOrder?> CreateOrder(PublicVoidFile file);

    ValueTask<PaywallOrder?> GetOrderStatus(Guid id);
}