using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IStatsReporter
{
    ValueTask<Bandwidth> GetBandwidth();
    ValueTask<Bandwidth> GetBandwidth(Guid id);
    ValueTask Delete(Guid id);
}