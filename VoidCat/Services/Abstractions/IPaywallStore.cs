using VoidCat.Model.Paywall;

namespace VoidCat.Services.Abstractions;

/// <summary>
/// Store for paywall configs
/// </summary>
public interface IPaywallStore : IBasicStore<PaywallConfig>
{
}