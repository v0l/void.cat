﻿namespace VoidCat.Model.Paywall;

public enum PaywallServices
{
    None,
    Strike
}

public abstract record PaywallConfig(PaywallServices Service, PaywallMoney Cost);

public record StrikePaywallConfig(PaywallServices Service, PaywallMoney Cost) : PaywallConfig(Service, Cost)
{
    public string Handle { get; init; }
}