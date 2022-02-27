using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers;

[Route("user")]
public class UserController : Controller
{
    private readonly IUserStore _store;
    private readonly IUserUploadsStore _userUploads;

    public UserController(IUserStore store, IUserUploadsStore userUploads)
    {
        _store = store;
        _userUploads = userUploads;
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

        var user = await _store.Get<PublicVoidUser>(id.FromBase58Guid());
        if (!(user?.Flags.HasFlag(VoidUserFlags.PublicProfile) ?? false)) return default;

        return user;
    }

    [HttpPost]
    [Route("{id}")]
    public async Task<IActionResult> UpdateUser([FromRoute] string id, [FromBody] PublicVoidUser user)
    {
        var loggedUser = HttpContext.GetUserId();
        var requestedId = id.FromBase58Guid();
        if (requestedId != loggedUser)
        {
            return Unauthorized();
        }

        // check requested user is same as user obj
        if (requestedId != user.Id)
        {
            return BadRequest();
        }

        await _store.Update(user);
        return Ok();
    }

    [HttpPost]
    [Route("{id}/files")]
    public async Task<RenderedResults<PublicVoidFile>?> ListUserFiles([FromRoute] string id, [FromBody] PagedRequest request)
    {
        var loggedUser = HttpContext.GetUserId();
        var gid = id.FromBase58Guid();
        var user = await _store.Get<PublicVoidUser>(gid);
        if (!(user?.Flags.HasFlag(VoidUserFlags.PublicUploads) ?? false) && loggedUser != gid) return default;
        
        var results = await _userUploads.ListFiles(id.FromBase58Guid(), request);
        return await results.GetResults();
    }
}
