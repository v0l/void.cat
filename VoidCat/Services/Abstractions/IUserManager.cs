using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IUserManager
{
    ValueTask<VoidUser> Login(string username, string password);
    ValueTask<VoidUser> Register(string username, string password);
}
