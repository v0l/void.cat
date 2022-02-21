namespace VoidCat.Model.Paywall;

public enum PaywallOrderStatus : byte
{
    Unpaid,
    Paid,
    Expired
}

public record PaywallOrder(Guid Id, PaywallMoney Price, PaywallOrderStatus Status);

public record LightningPaywallOrder(Guid Id, PaywallMoney Price, PaywallOrderStatus Status, string LnInvoice,
    DateTimeOffset Expire) : PaywallOrder(Id, Price, Status);