using VoidCat.Model.Paywall;

namespace VoidCat.Services.Abstractions;

public interface IPaywallFactory
{
    ValueTask<IPaywallProvider> CreateProvider(PaywallServices svc);
}