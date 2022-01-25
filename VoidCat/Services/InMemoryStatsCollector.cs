namespace VoidCat.Services
{
    public class InMemoryStatsCollector : IStatsCollector
    {
        public ValueTask TrackIngress(Guid id, ulong amount)
        {
            throw new NotImplementedException();
        }

        public ValueTask TrackEgress(Guid id, ulong amount)
        {
            throw new NotImplementedException();
        }
    }
}
