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

    public AuthController(IUserManager userStore, VoidSettings settings)
    {
        _manager = userStore;
        _settings = settings;
    }

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


    public class LoginRequest
    {
        public LoginRequest(string username, string password)
        {
            Username = username;
            Password = password;
        }

        [Required]
        [EmailAddress]
        public string Username { get; init; }
        
        [Required]
        [MinLength(6)]
        public string Password { get; init; }
    }

    public record LoginResponse(string? Jwt, string? Error = null, VoidUser? Profile = null);
}
