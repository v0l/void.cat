using VoidCat.Services.Abstractions;

namespace VoidCat.Model;

public sealed record EgressRequest(Guid Id, IEnumerable<RangeRequest> Ranges)
{
}