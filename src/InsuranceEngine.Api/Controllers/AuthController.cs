using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InsuranceEngine.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InsuranceEngine.Api.Controllers;

/// <summary>JWT-based authentication endpoints.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly InsuranceDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(InsuranceDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>Login and receive a JWT token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return Unauthorized(new { error = "Username and password are required." });

        var user = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.Username == req.Username);

        if (user == null || !VerifyPassword(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid username or password." });

        var token = GenerateJwtToken(user.Username, user.Role);
        return Ok(new LoginResponse
        {
            Token = token,
            Username = user.Username,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        });
    }

    private string GenerateJwtToken(string username, string role)
    {
        var key = _config["Jwt:Key"] ?? "PrecisionProDefaultSecretKey2026!";
        var issuer = _config["Jwt:Issuer"] ?? "PrecisionPro";
        var audience = _config["Jwt:Audience"] ?? "PrecisionProUsers";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    internal static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        var combined = new byte[48];
        Buffer.BlockCopy(salt, 0, combined, 0, 16);
        Buffer.BlockCopy(hash, 0, combined, 16, 32);
        return Convert.ToBase64String(combined);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var combined = Convert.FromBase64String(storedHash);
            var salt = new byte[16];
            var hash = new byte[32];
            Buffer.BlockCopy(combined, 0, salt, 0, 16);
            Buffer.BlockCopy(combined, 16, hash, 0, 32);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var testHash = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(hash, testHash);
        }
        catch
        {
            return false;
        }
    }
}

public record LoginRequest(string Username, string Password);
public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}
