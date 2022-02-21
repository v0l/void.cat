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
            var user = await _manager.Login(req.Username, req.Password);
            var token = CreateToken(user);
            var tokenWriter = new JwtSecurityTokenHandler();
            return new(tokenWriter.WriteToken(token), null);
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
            var newUser = await _manager.Register(req.Username, req.Password);
            var token = CreateToken(newUser);
            var tokenWriter = new JwtSecurityTokenHandler();
            return new(tokenWriter.WriteToken(token), null);
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

        var claims = new Claim[]
        {
            new(ClaimTypes.Sid, user.Id.ToString()),
            new(ClaimTypes.Expiration, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new(ClaimTypes.AuthorizationDecision, string.Join(",", user.Roles))
        };

        return new JwtSecurityToken(_settings.JwtSettings.Issuer, claims: claims, expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: credentials);
    }


    public record LoginRequest(string Username, string Password);

    public record LoginResponse(string? Jwt, string? Error = null);
}
