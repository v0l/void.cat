namespace VoidCat.Model.Paywall;

public enum PaywallStatus : byte
{
    Unpaid,
    Paid,
    Expired
}

public abstract record PaywallOrder(Guid Id, PaywallMoney Price, PaywallStatus Status);
public record LightningPaywallOrder(Guid Id, PaywallMoney Price, PaywallStatus Status, string LnInvoice) : PaywallOrder(Id, Price, Status);
