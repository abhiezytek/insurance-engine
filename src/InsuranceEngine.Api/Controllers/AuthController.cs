using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Authorization;
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
    private readonly IActivityAuditService _audit;

    public AuthController(InsuranceDbContext db, IConfiguration config, IActivityAuditService audit)
    {
        _db = db;
        _config = config;
        _audit = audit;
    }

    /// <summary>Login and receive a JWT token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        {
            await LogLoginAttempt(req.Username ?? "", false, "Empty credentials");
            await _audit.LogAsync("Auth", "LoginFailed", recordId: req.Username, status: "Failure", errorMessage: "Empty credentials");
            return Unauthorized(new { error = "Username and password are required." });
        }

        var user = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.Username == req.Username);

        if (user == null || !VerifyPassword(req.Password, user.PasswordHash))
        {
            await LogLoginAttempt(req.Username, false, "Invalid credentials");
            await _audit.LogAsync("Auth", "LoginFailed", recordId: req.Username, status: "Failure", errorMessage: "Invalid credentials");
            return Unauthorized(new { error = "Invalid username or password." });
        }

        await LogLoginAttempt(user.Username, true, null);

        // Check if user must change password before accessing the system
        if (user.ForceChangePassword)
        {
            var tempToken = GenerateTempToken(user.Username, user.Role);
            return Ok(new LoginResponse
            {
                RequiresPasswordChange = true,
                TempToken = tempToken,
                Message = "You must change your password before accessing the system."
            });
        }

        await _audit.LogAsync("Auth", "Login", recordId: user.Username);

        var token = GenerateJwtToken(user.Username, user.Role);
        return Ok(new LoginResponse
        {
            Token = token,
            Username = user.Username,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        });
    }

    /// <summary>Change password (supports both forced and voluntary changes).</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { error = "Invalid token." });

        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return BadRequest(new { error = "User not found." });

        // Validate new password policy: min 8 chars, 1 uppercase, 1 number, 1 special
        if (!IsValidPassword(req.NewPassword))
            return BadRequest(new { error = "Password must be at least 8 characters with uppercase, number and special character." });

        // New password cannot be a default password
        if (req.NewPassword == "admin123" || req.NewPassword == "Welcome@123")
            return BadRequest(new { error = "Cannot use default password." });

        // If not a forced change, verify current password
        if (!user.ForceChangePassword)
        {
            if (string.IsNullOrWhiteSpace(req.CurrentPassword))
                return BadRequest(new { error = "Current password is required." });
            if (!VerifyPassword(req.CurrentPassword, user.PasswordHash))
                return BadRequest(new { error = "Current password incorrect." });
        }

        user.PasswordHash = HashPassword(req.NewPassword);
        user.ForceChangePassword = false;
        user.PasswordChangedOn = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("Auth", "PasswordChanged", recordId: user.Username);

        // Return full JWT token so user can proceed immediately
        var token = GenerateJwtToken(user.Username, user.Role);
        return Ok(new
        {
            token,
            username = user.Username,
            role = user.Role,
            expiresAt = DateTime.UtcNow.AddHours(8),
            message = "Password changed successfully."
        });
    }

    /// <summary>Get login history.</summary>
    [HttpGet("login-history")]
    [ProducesResponseType(typeof(List<Models.LoginHistory>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLoginHistory([FromQuery] int top = 50)
    {
        var history = await _db.LoginHistories
            .OrderByDescending(h => h.LoginTime)
            .Take(top)
            .ToListAsync();
        return Ok(history);
    }

    private async Task LogLoginAttempt(string username, bool success, string? failureReason)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        _db.LoginHistories.Add(new Models.LoginHistory
        {
            Username = username,
            IpAddress = ipAddress,
            Success = success,
            FailureReason = failureReason
        });
        await _db.SaveChangesAsync();
    }

    private string GenerateJwtToken(string username, string role)
    {
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT key is not configured. Set Jwt:Key in appsettings or environment variables.");
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

    /// <summary>Generate a limited-scope temp token for password change only (15-minute expiry).</summary>
    private string GenerateTempToken(string username, string role)
    {
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT key is not configured. Set Jwt:Key in appsettings or environment variables.");
        var issuer = _config["Jwt:Issuer"] ?? "PrecisionPro";
        var audience = _config["Jwt:Audience"] ?? "PrecisionProUsers";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("scope", "password-change-only")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Validate password policy: min 8 chars, 1 uppercase, 1 number, 1 special character.</summary>
    private static bool IsValidPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;
        if (!Regex.IsMatch(password, @"[A-Z]"))
            return false;
        if (!Regex.IsMatch(password, @"\d"))
            return false;
        if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
            return false;
        return true;
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

    internal static bool VerifyPassword(string password, string storedHash)
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
public record ChangePasswordRequest(string? CurrentPassword, string NewPassword);
public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
    public bool RequiresPasswordChange { get; init; }
    public string? TempToken { get; init; }
    public string? Message { get; init; }
}
