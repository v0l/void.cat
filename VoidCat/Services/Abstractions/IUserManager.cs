namespace VoidCat.Services.Abstractions;

public interface IUserManager
{
    ValueTask<VoidUser> Get(string email, string password);
    ValueTask<VoidUser> Get(Guid id);
    ValueTask Set(VoidUser user);
}

public sealed record VoidUser(Guid Id, string Email, string PasswordHash); 