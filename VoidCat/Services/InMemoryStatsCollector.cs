using Microsoft.Extensions.Caching.Memory;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services;

public class InMemoryStatsCollector : IStatsCollector
{
    private static Guid _global = new Guid("{A98DFDCC-C4E1-4D42-B818-912086FC6157}");
    private readonly IMemoryCache _cache;

    public InMemoryStatsCollector(IMemoryCache cache)
    {
        _cache = cache;
    }

    public ValueTask TrackIngress(Guid id, ulong amount)
    {
        Incr(IngressKey(id), amount);
        Incr(IngressKey(_global), amount);
        return ValueTask.CompletedTask;
    }

    public ValueTask TrackEgress(Guid id, ulong amount)
    {
        Incr(EgressKey(id), amount);
        Incr(EgressKey(_global), amount);
        return ValueTask.CompletedTask;
    }

    public ValueTask<Bandwidth> GetBandwidth()
        => ValueTask.FromResult(GetBandwidthInternal(_global));

    public ValueTask<Bandwidth> GetBandwidth(Guid id)
        => ValueTask.FromResult(GetBandwidthInternal(id));

    private Bandwidth GetBandwidthInternal(Guid id)
    {
        var i = _cache.Get(IngressKey(id)) as ulong?;
        var o = _cache.Get(EgressKey(id)) as ulong?;
        return new(i ?? 0UL, o ?? 0UL);
    }

    private void Incr(string k, ulong amount)
    {
        ulong v;
        _cache.TryGetValue(k, out v);
        _cache.Set(k, v + amount);
    }

    private string IngressKey(Guid id) => $"stats:ingress:{id}";
    private string EgressKey(Guid id) => $"stats:egress:{id}";
}