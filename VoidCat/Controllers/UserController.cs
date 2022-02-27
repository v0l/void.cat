using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers;

[Route("user")]
public class UserController : Controller
{
    private readonly IUserStore _store;

    public UserController(IUserStore store)
    {
        _store = store;
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<VoidUser?> GetUser([FromRoute] string id)
    {
        var loggedUser = HttpContext.GetUserId();
        var requestedId = id.FromBase58Guid();
        if (loggedUser == requestedId)
        {
            return await _store.Get<PrivateVoidUser>(id.FromBase58Guid());
        }
        else
        {
            return await _store.Get<PublicVoidUser>(id.FromBase58Guid());
        }
    }
}
