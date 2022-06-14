namespace VoidCat.Model;

/// <summary>
/// I/O bandwidth model
/// </summary>
/// <param name="Ingress"></param>
/// <param name="Egress"></param>
public sealed record Bandwidth(ulong Ingress, ulong Egress);

/// <summary>
/// I/O bandwidth model at a specific time
/// </summary>
/// <param name="Time"></param>
/// <param name="Ingress"></param>
/// <param name="Egress"></param>
public sealed record BandwidthPoint(DateTime Time, ulong Ingress, ulong Egress);