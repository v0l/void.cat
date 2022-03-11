using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers;

[Route("auth")]
public class AuthController : Controller
{
    private readonly IUserManager _manager;
    private readonly VoidSettings _settings;
    private readonly ICaptchaVerifier _captchaVerifier;

    public AuthController(IUserManager userStore, VoidSettings settings, ICaptchaVerifier captchaVerifier)
    {
        _manager = userStore;
        _settings = settings;
        _captchaVerifier = captchaVerifier;
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
            var token = CreateToken(user);
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
            var token = CreateToken(newUser);
            var tokenWriter = new JwtSecurityTokenHandler();
            return new(tokenWriter.WriteToken(token), Profile: newUser.ToPublic());
        }
        catch (Exception ex)
        {
            return new(null, ex.Message);
        }
    }

    private JwtSecurityToken CreateToken(VoidUser user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.JwtSettings.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>()
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds().ToString()),
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

        [Required]
        [EmailAddress]
        public string Username { get; }
        
        [Required]
        [MinLength(6)]
        public string Password { get; }

        public string? Captcha { get; init; }
    }

    public sealed record LoginResponse(string? Jwt, string? Error = null, VoidUser? Profile = null);
}
