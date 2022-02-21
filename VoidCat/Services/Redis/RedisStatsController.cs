using StackExchange.Redis;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Redis;

public class RedisStatsController : IStatsReporter, IStatsCollector
{
    private const string GlobalEgress = "stats:egress:global";
    private const string GlobalIngress = "stats:ingress:global";
    private readonly IDatabase _redis;

    public RedisStatsController(IDatabase redis)
    {
        _redis = redis;
    }

    public async ValueTask<Bandwidth> GetBandwidth()
    {
        var egress = _redis.StringGetAsync(GlobalEgress);
        var ingress = _redis.StringGetAsync(GlobalIngress);
        await Task.WhenAll(egress, ingress);

        return new((ulong)ingress.Result, (ulong)egress.Result);
    }

    public async ValueTask<Bandwidth> GetBandwidth(Guid id)
    {
        var egress = _redis.StringGetAsync(formatEgressKey(id));
        var ingress = _redis.StringGetAsync(formatIngressKey(id));
        await Task.WhenAll(egress, ingress);

        return new((ulong)ingress.Result, (ulong)egress.Result);
    }

    public async ValueTask TrackIngress(Guid id, ulong amount)
    {
        await Task.WhenAll(
            _redis.StringIncrementAsync(GlobalIngress, amount),
            _redis.StringIncrementAsync(formatIngressKey(id), amount));
    }

    public async ValueTask TrackEgress(Guid id, ulong amount)
    {
        await Task.WhenAll(
            _redis.StringIncrementAsync(GlobalEgress, amount),
            _redis.StringIncrementAsync(formatEgressKey(id), amount));
    }

    private RedisKey formatIngressKey(Guid id) => $"stats:{id}:ingress";
    private RedisKey formatEgressKey(Guid id) => $"stats:{id}:egress";
}
