using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VoidCat.Database;
using VoidCat.Model;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Files;
using UserFlags = VoidCat.Database.UserFlags;

namespace VoidCat.Controllers;

[Route("user/{id}")]
public class UserController : Controller
{
    private readonly IUserStore _store;
    private readonly IUserUploadsStore _userUploads;
    private readonly IEmailVerification _emailVerification;
    private readonly FileInfoManager _fileInfoManager;

    public UserController(IUserStore store, IUserUploadsStore userUploads, IEmailVerification emailVerification,
        FileInfoManager fileInfoManager)
    {
        _store = store;
        _userUploads = userUploads;
        _emailVerification = emailVerification;
        _fileInfoManager = fileInfoManager;
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
        var user = await _store.Get(requestedId);
        if (user == default) return NotFound();
        if (loggedUser != requestedId && !user.Flags.HasFlag(UserFlags.PublicProfile))
            return NotFound();

        var isMyProfile = requestedId == user.Id;
        return Json(user!.ToApiUser(isMyProfile));
    }

    /// <summary>
    /// Update profile settings
    /// </summary>
    /// 
    /// <param name="id">User id</param>
    /// <param name="user"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> UpdateUser([FromRoute] string id, [FromBody] ApiUser user)
    {
        var loggedUser = await GetAuthorizedUser(id);
        if (loggedUser == default) return Unauthorized();

        if (!loggedUser.Flags.HasFlag(UserFlags.EmailVerified)) return Forbid();

        loggedUser.Avatar = user.Avatar;
        loggedUser.DisplayName = user.Name ?? "void user";
        loggedUser.Flags = UserFlags.EmailVerified | (user.PublicProfile ? UserFlags.PublicProfile : 0) |
                           (user.PublicUploads ? UserFlags.PublicUploads : 0);

        await _store.UpdateProfile(loggedUser);

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
            !user.Flags.HasFlag(UserFlags.PublicUploads)) return Forbid();

        var results = await _userUploads.ListFiles(id.FromBase58Guid(), request);
        var files = await results.Results.ToListAsync();
        var fileInfo = await _fileInfoManager.Get(files.ToArray(), false);
        return Json(new RenderedResults<VoidFileResponse>()
        {
            PageSize = results.PageSize,
            Page = results.Page,
            TotalResults = results.TotalResults,
            Results = fileInfo.ToList()
        });
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

        var isEmailVerified = (user?.Flags.HasFlag(UserFlags.EmailVerified) ?? false);
        if (isEmailVerified) return UnprocessableEntity();

        await _emailVerification.SendNewCode(user!);
        return Accepted();
    }

    /// <summary>
    /// Confirm email verification code
    /// </summary>
    /// <param name="id">User id to verify</param>
    /// <param name="req">Verification code to check</param>
    /// <returns></returns>
    [HttpPost]
    [Route("verify")]
    public async Task<IActionResult> VerifyCode([FromRoute] string id, [FromBody] VerifyCodeRequest req)
    {
        var user = await GetAuthorizedUser(id);
        if (user == default) return Unauthorized();

        if (!await _emailVerification.VerifyCode(user, req.Code)) return BadRequest();

        user.Flags |= UserFlags.EmailVerified;
        await _store.UpdateProfile(user);
        return Accepted();
    }

    private async Task<User?> GetAuthorizedUser(string id)
    {
        var loggedUser = HttpContext.GetUserId();
        var gid = id.FromBase58Guid();
        var user = await _store.Get(gid);
        return user?.Id != loggedUser ? default : user;
    }

    private async Task<User?> GetRequestedUser(string id)
    {
        var gid = id.FromBase58Guid();
        return await _store.Get(gid);
    }

    public class VerifyCodeRequest
    {
        [JsonProperty("code")]
        [JsonConverter(typeof(Base58GuidConverter))]
        public Guid Code { get; init; }
    }
}
