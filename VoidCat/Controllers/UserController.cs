using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers;

[Route("user/{id}")]
public class UserController : Controller
{
    private readonly IUserStore _store;
    private readonly IUserUploadsStore _userUploads;
    private readonly IEmailVerification _emailVerification;

    public UserController(IUserStore store, IUserUploadsStore userUploads, IEmailVerification emailVerification)
    {
        _store = store;
        _userUploads = userUploads;
        _emailVerification = emailVerification;
    }

    [HttpGet]
    public async Task<IActionResult> GetUser([FromRoute] string id)
    {
        var loggedUser = HttpContext.GetUserId();
        var isMe = id.Equals("me", StringComparison.InvariantCultureIgnoreCase);
        if (isMe && !loggedUser.HasValue) return Unauthorized();
        
        var requestedId = isMe ? loggedUser!.Value : id.FromBase58Guid();
        if (loggedUser == requestedId)
        {
            return Json(await _store.Get<PrivateVoidUser>(requestedId));
        }

        var user = await _store.Get<PublicVoidUser>(requestedId);
        if (!(user?.Flags.HasFlag(VoidUserFlags.PublicProfile) ?? false)) return NotFound();

        return Json(user);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateUser([FromRoute] string id, [FromBody] PublicVoidUser user)
    {
        var loggedUser = await GetAuthorizedUser(id);
        if (loggedUser == default) return Unauthorized();

        if (!loggedUser.Flags.HasFlag(VoidUserFlags.EmailVerified)) return Forbid();

        await _store.UpdateProfile(user);
        return Ok();
    }

    [HttpPost]
    [Route("files")]
    public async Task<IActionResult> ListUserFiles([FromRoute] string id,
        [FromBody] PagedRequest request)
    {
        var loggedUser = HttpContext.GetUserId();
        var isAdmin = HttpContext.IsRole(Roles.Admin);

        var user = await GetRequestedUser(id);
        if (user == default) return NotFound();

        // not logged in user files, check public flag
        var canViewUploads = loggedUser == user.Id || isAdmin;
        if (!canViewUploads &&
            !user.Flags.HasFlag(VoidUserFlags.PublicUploads)) return Forbid();

        var results = await _userUploads.ListFiles(id.FromBase58Guid(), request);
        return Json(await results.GetResults());
    }

    [HttpGet]
    [Route("verify")]
    public async Task<IActionResult> SendVerificationCode([FromRoute] string id)
    {
        var user = await GetAuthorizedUser(id);
        if (user == default) return Unauthorized();

        var isEmailVerified = (user?.Flags.HasFlag(VoidUserFlags.EmailVerified) ?? false);
        if (isEmailVerified) return UnprocessableEntity();

        await _emailVerification.SendNewCode(user!);
        return Accepted();
    }

    [HttpPost]
    [Route("verify")]
    public async Task<IActionResult> VerifyCode([FromRoute] string id, [FromBody] string code)
    {
        var user = await GetAuthorizedUser(id);
        if (user == default) return Unauthorized();

        var token = code.FromBase58Guid();
        if (!await _emailVerification.VerifyCode(user, token)) return BadRequest();

        user.Flags |= VoidUserFlags.EmailVerified;
        await _store.Set(user);
        return Accepted();
    }

    private async Task<InternalVoidUser?> GetAuthorizedUser(string id)
    {
        var loggedUser = HttpContext.GetUserId();
        var gid = id.FromBase58Guid();
        var user = await _store.Get<InternalVoidUser>(gid);
        return user?.Id != loggedUser ? default : user;
    }

    private async Task<InternalVoidUser?> GetRequestedUser(string id)
    {
        var gid = id.FromBase58Guid();
        return await _store.Get<InternalVoidUser>(gid);
    }
}