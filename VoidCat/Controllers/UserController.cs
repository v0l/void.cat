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

    /// <summary>
    /// Return user profile
    /// </summary>
    /// <remarks>
    /// You do not need to be logged in to return a user profile if their profile is set to public.
    ///
    /// You may also use `me` as the `id` to get the logged in users profile.
    /// </remarks>
    /// <param name="id">User id to load</param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetUser([FromRoute] string id)
    {
        var loggedUser = HttpContext.GetUserId();
        var isMe = id.Equals("me", StringComparison.InvariantCultureIgnoreCase);
        if (isMe && !loggedUser.HasValue) return Unauthorized();

        var requestedId = isMe ? loggedUser!.Value : id.FromBase58Guid();
        if (loggedUser == requestedId)
        {
            var pUser = await _store.Get<PrivateVoidUser>(requestedId);
            if (pUser == default) return NotFound();
            return Json(pUser);
        }

        var user = await _store.Get<PublicVoidUser>(requestedId);
        if (!(user?.Flags.HasFlag(VoidUserFlags.PublicProfile) ?? false)) return NotFound();

        return Json(user);
    }

    /// <summary>
    /// Update profile settings
    /// </summary>
    /// 
    /// <param name="id">User id</param>
    /// <param name="user"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> UpdateUser([FromRoute] string id, [FromBody] PublicVoidUser user)
    {
        var loggedUser = await GetAuthorizedUser(id);
        if (loggedUser == default) return Unauthorized();

        if (!loggedUser.Flags.HasFlag(VoidUserFlags.EmailVerified)) return Forbid();

        await _store.UpdateProfile(user);
        return Ok();
    }

    /// <summary>
    /// Return a list of files which the user has uploaded
    /// </summary>
    /// <remarks>
    /// This will return files if the profile has public uploads set on their profile.
    /// Otherwise you can return your own uploaded files if you are logged in. 
    /// </remarks>
    /// <param name="id">User id</param>
    /// <param name="request">Page request</param>
    /// <returns></returns>
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

    /// <summary>
    /// Send a verification code for a specific user
    /// </summary>
    /// <param name="id">User id to send code for</param>
    /// <returns></returns>
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

    /// <summary>
    /// Confirm email verification code
    /// </summary>
    /// <param name="id">User id to verify</param>
    /// <param name="code">Verification code to check</param>
    /// <returns></returns>
    [HttpPost]
    [Route("verify")]
    public async Task<IActionResult> VerifyCode([FromRoute] string id, [FromBody] string code)
    {
        var user = await GetAuthorizedUser(id);
        if (user == default) return Unauthorized();

        var token = code.FromBase58Guid();
        if (!await _emailVerification.VerifyCode(user, token)) return BadRequest();

        user.Flags |= VoidUserFlags.EmailVerified;
        await _store.Set(user.Id, user);
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