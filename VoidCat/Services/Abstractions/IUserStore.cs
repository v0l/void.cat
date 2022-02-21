using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IUserStore
{
    ValueTask<Guid?> LookupUser(string email);
    ValueTask<VoidUser?> Get(Guid id);
    ValueTask Set(VoidUser user);
    IAsyncEnumerable<VoidUser> ListUsers(CancellationToken cts);
}