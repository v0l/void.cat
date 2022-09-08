using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using VoidCat.Model;
using VoidCat.Model.User;
using VoidCat.Services.Abstractions;
using VoidCat.Services.Users;

namespace VoidCat.Controllers;

[Route("auth")]
public class AuthController : Controller
{
    private readonly UserManager _manager;
    private readonly VoidSettings _settings;
    private readonly ICaptchaVerifier _captchaVerifier;
    private readonly IApiKeyStore _apiKeyStore;
    private readonly IUserStore _userStore;

    public AuthController(UserManager userManager, VoidSettings settings, ICaptchaVerifier captchaVerifier,
        IApiKeyStore apiKeyStore,
        IUserStore userStore)
    {
        _manager = userManager;
        _settings = settings;
        _captchaVerifier = captchaVerifier;
        _apiKeyStore = apiKeyStore;
        _userStore = userStore;
    }

    /// <summary>
    /// Login to a user account
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("login")]
    public async Task<LoginResponse> Login([FromBody] LoginRequest req)
    {
        try
        {
            if (!TryValidateModel(req))
            {
                var error = ControllerContext.ModelState.FirstOrDefault().Value?.Errors.FirstOrDefault()?.ErrorMessage;
                return new(null, error);
            }

            // check captcha
            if (!await _captchaVerifier.Verify(req.Captcha))
            {
                return new(null, "Captcha verification failed");
            }

            var user = await _manager.Login(req.Username, req.Password);
            var token = CreateToken(user, DateTime.UtcNow.AddHours(12));
            var tokenWriter = new JwtSecurityTokenHandler();
            return new(tokenWriter.WriteToken(token), Profile: user.ToPublic());
        }
        catch (Exception ex)
        {
            return new(null, ex.Message);
        }
    }

    /// <summary>
    /// Register a new account
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("register")]
    public async Task<LoginResponse> Register([FromBody] LoginRequest req)
    {
        try
        {
            if (!TryValidateModel(req))
            {
                var error = ControllerContext.ModelState.FirstOrDefault().Value?.Errors.FirstOrDefault()?.ErrorMessage;
                return new(null, error);
            }

            // check captcha
            if (!await _captchaVerifier.Verify(req.Captcha))
            {
                return new(null, "Captcha verification failed");
            }

            var newUser = await _manager.Register(req.Username, req.Password);
            var token = CreateToken(newUser, DateTime.UtcNow.AddHours(12));
            var tokenWriter = new JwtSecurityTokenHandler();
            return new(tokenWriter.WriteToken(token), Profile: newUser.ToPublic());
        }
        catch (Exception ex)
        {
            return new(null, ex.Message);
        }
    }

    /// <summary>
    /// Start OAuth2 authorize flow 
    /// </summary>
    /// <param name="provider">OAuth provider</param>
    /// <returns></returns>
    [HttpGet]
    [Route("{provider}")]
    public IActionResult Authorize([FromRoute] string provider)
    {
        return Redirect(_manager.Authorize(provider).ToString());
    }

    /// <summary>
    /// Authorize user from OAuth2 code grant
    /// </summary>
    /// <param name="code">Code used to generate access token</param>
    /// <param name="provider">OAuth provider</param>
    /// <returns></returns>
    [HttpGet]
    [Route("{provider}/token")]
    public async Task<IActionResult> Token([FromRoute] string provider, [FromQuery] string code)
    {
        var newUser = await _manager.LoginOrRegister(code, provider);
        var token = CreateToken(newUser, DateTime.UtcNow.AddHours(12));
        var tokenWriter = new JwtSecurityTokenHandler();

        return Redirect($"/login#{tokenWriter.WriteToken(token)}");
    }

    /// <summary>
    /// List api keys for the user
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("api-key")]
    public async Task<IActionResult> ListApiKeys()
    {
        var uid = HttpContext.GetUserId();
        if (uid == default) return Unauthorized();

        return Json(await _apiKeyStore.ListKeys(uid.Value));
    }

    /// <summary>
    /// Create a new API key for the logged in user
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("api-key")]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        var uid = HttpContext.GetUserId();
        if (uid == default) return Unauthorized();

        var user = await _userStore.Get(uid.Value);
        if (user == default) return Unauthorized();

        var expiry = DateTime.SpecifyKind(request.Expiry, DateTimeKind.Utc);
        if (expiry > DateTime.UtcNow.AddYears(1))
        {
            return BadRequest();
        }

        var key = new ApiKey()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = new JwtSecurityTokenHandler().WriteToken(CreateToken(user, expiry)),
            Expiry = expiry
        };

        await _apiKeyStore.Add(key.Id, key);
        return Json(key);
    }

    private JwtSecurityToken CreateToken(User user, DateTime expiry)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.JwtSettings.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>()
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Exp, new DateTimeOffset(expiry).ToUnixTimeSeconds().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        claims.AddRange(user.Roles.Select(a => new Claim(ClaimTypes.Role, a)));

        return new JwtSecurityToken(_settings.JwtSettings.Issuer, claims: claims,
            signingCredentials: credentials);
    }

    public sealed class LoginRequest
    {
        public LoginRequest(string username, string password)
        {
            Username = username;
            Password = password;
        }

        [Required] [EmailAddress] public string Username { get; }

        [Required] [MinLength(6)] public string Password { get; }

        public string? Captcha { get; init; }
    }

    public sealed record LoginResponse(string? Jwt, string? Error = null, User? Profile = null);

    public sealed record CreateApiKeyRequest(DateTime Expiry);
}