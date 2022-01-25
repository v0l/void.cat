using System.Collections.Concurrent;

namespace VoidCat.Services
{
    public class InMemoryStatsCollector : IStatsCollector
    {
        private readonly ConcurrentDictionary<Guid, ulong> _ingress = new();
        private readonly ConcurrentDictionary<Guid, ulong> _egress = new();
        
        public ValueTask TrackIngress(Guid id, ulong amount)
        {
            if (_ingress.ContainsKey(id) && _ingress.TryGetValue(id, out var v))
            {
                _ingress.TryUpdate(id, v + amount, v);
            }
            else
            {
                _ingress.TryAdd(id, amount);
            }
            return ValueTask.CompletedTask;
        }

        public ValueTask TrackEgress(Guid id, ulong amount)
        {
            if (_egress.ContainsKey(id) && _egress.TryGetValue(id, out var v))
            {
                _egress.TryUpdate(id, v + amount, v);
            }
            else
            {
                _egress.TryAdd(id, amount);
            }
            return ValueTask.CompletedTask;
        }
    }
}
