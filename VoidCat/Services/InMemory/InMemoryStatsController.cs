using Microsoft.Extensions.Caching.Memory;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.InMemory;

public class InMemoryStatsController : IStatsCollector, IStatsReporter
{
    private static readonly Guid Global = new Guid("{A98DFDCC-C4E1-4D42-B818-912086FC6157}");
    private readonly IMemoryCache _cache;

    public InMemoryStatsController(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public ValueTask TrackIngress(Guid id, ulong amount)
    {
        Incr(IngressKey(id), amount);
        Incr(IngressKey(Global), amount);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask TrackEgress(Guid id, ulong amount)
    {
        Incr(EgressKey(id), amount);
        Incr(EgressKey(Global), amount);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<Bandwidth> GetBandwidth()
        => ValueTask.FromResult(GetBandwidthInternal(Global));

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<BandwidthPoint>> GetBandwidth(DateTime start, DateTime end)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<Bandwidth> GetBandwidth(Guid id)
        => ValueTask.FromResult(GetBandwidthInternal(id));

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<BandwidthPoint>> GetBandwidth(Guid id, DateTime start, DateTime end)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask Delete(Guid id)
    {
        _cache.Remove(EgressKey(id));
        return ValueTask.CompletedTask;
    }

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