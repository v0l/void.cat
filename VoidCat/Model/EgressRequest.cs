namespace VoidCat.Model;

public sealed record EgressRequest(Guid Id, IEnumerable<RangeRequest> Ranges);

public sealed record EgressResult(Uri? Redirect = null);