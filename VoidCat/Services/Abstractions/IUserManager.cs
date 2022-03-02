using VoidCat.Model;

namespace VoidCat.Services.Abstractions;

public interface IUserManager
{
    ValueTask<InternalVoidUser> Login(string username, string password);
    ValueTask<InternalVoidUser> Register(string username, string password);
}
