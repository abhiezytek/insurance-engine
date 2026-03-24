using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Security;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Controllers;

/// <summary>User management endpoints for users, roles, and access matrix.</summary>
[ApiController]
[Route("api/usermgmt")]
[Produces("application/json")]
[Authorize(Policy = "CanManageUsers")]
[RequireRoleHeader("Admin", "SuperAdmin")]
public class UserMgmtController : ControllerBase
{
    private readonly InsuranceDbContext _db;
    private readonly ILogger<UserMgmtController> _logger;
    private readonly IActivityAuditService _audit;

    public UserMgmtController(InsuranceDbContext db, ILogger<UserMgmtController> logger, IActivityAuditService audit)
    {
        _db = db;
        _logger = logger;
        _audit = audit;
    }

    // -------------------------------------------------------------------------
    // User CRUD
    // -------------------------------------------------------------------------

    /// <summary>Create a new user.</summary>
    [HttpPost("users")]
    [ProducesResponseType(typeof(UserMaster), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUser([FromBody] UmCreateUserDto dto)
    {
        var user = new UserMaster
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Mobile = dto.Mobile,
            EmployeeId = dto.EmployeeId,
            Department = dto.Department,
            PasswordHash = HashPassword(dto.Password ?? "Welcome@123"),
            Status = dto.Status ?? "Active",
            ForceChangePassword = true,
            ClientId = dto.ClientId,
            CreatedBy = User.Identity?.Name ?? "System"
        };
        _db.UserMasters.Add(user);
        await _db.SaveChangesAsync();

        if (dto.RoleIds is { Count: > 0 })
        {
            foreach (var roleId in dto.RoleIds)
                _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
            await _db.SaveChangesAsync();
        }

        _logger.LogInformation("User created Id={UserId} Email={Email}", user.Id, user.Email);
        await _audit.LogAsync("UserMgmt", "UserCreated", recordId: user.Id.ToString(), newValue: user.Email);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    /// <summary>List users with optional search, status, and role filters.</summary>
    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? roleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Min(Math.Max(pageSize, 1), 100);
        page = Math.Max(page, 1);

        var query = _db.UserMasters
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                (u.EmployeeId != null && u.EmployeeId.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(u => u.Status == status);

        if (roleId.HasValue)
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId.Value));

        var totalCount = await query.CountAsync();
        var users = await query.OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
        return Ok(new { data = users, totalCount, page, pageSize });
    }

    /// <summary>Get a single user by ID.</summary>
    [HttpGet("users/{id:int}")]
    [ProducesResponseType(typeof(UserMaster), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _db.UserMasters
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        return Ok(user);
    }

    /// <summary>Update an existing user.</summary>
    [HttpPut("users/{id:int}")]
    [ProducesResponseType(typeof(UserMaster), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UmUpdateUserDto dto)
    {
        var user = await _db.UserMasters
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        if (dto.FullName is not null) user.FullName = dto.FullName;
        if (dto.Email is not null) user.Email = dto.Email;
        if (dto.Mobile is not null) user.Mobile = dto.Mobile;
        if (dto.Department is not null) user.Department = dto.Department;
        if (dto.EmployeeId is not null) user.EmployeeId = dto.EmployeeId;
        if (dto.Status is not null) user.Status = dto.Status;
        if (dto.ClientId.HasValue) user.ClientId = dto.ClientId.Value;

        if (dto.RoleIds is not null)
        {
            _db.UserRoles.RemoveRange(user.UserRoles);
            foreach (var roleId in dto.RoleIds)
                _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("User updated Id={UserId}", user.Id);
        await _audit.LogAsync("UserMgmt", "UserUpdated", recordId: user.Id.ToString());
        return Ok(user);
    }

    /// <summary>Deactivate (soft-delete) a user by setting status to Inactive.</summary>
    [HttpDelete("users/{id:int}")]
    [ProducesResponseType(typeof(UserMaster), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var user = await _db.UserMasters.FindAsync(id);
        if (user is null) return NotFound();
        user.Status = "Inactive";
        await _db.SaveChangesAsync();
        _logger.LogInformation("User deactivated Id={UserId}", user.Id);
        await _audit.LogAsync("UserMgmt", "UserDeactivated", recordId: user.Id.ToString());
        return Ok(user);
    }

    /// <summary>Reset a user's password.</summary>
    [HttpPut("users/{id:int}/reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] UmResetPasswordDto dto)
    {
        var user = await _db.UserMasters.FindAsync(id);
        if (user is null) return NotFound();
        user.PasswordHash = HashPassword(dto.NewPassword);
        user.ForceChangePassword = true;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Password reset for UserId={UserId}", user.Id);
        await _audit.LogAsync("Auth", "PasswordReset", recordId: user.Id.ToString());
        return Ok(new { message = "Password has been reset." });
    }

    // -------------------------------------------------------------------------
    // Role CRUD
    // -------------------------------------------------------------------------

    /// <summary>Create a new role.</summary>
    [HttpPost("roles")]
    [ProducesResponseType(typeof(RoleMaster), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRole([FromBody] UmCreateRoleDto dto)
    {
        var role = new RoleMaster
        {
            RoleName = dto.RoleName,
            Description = dto.Description,
            IsActive = true
        };
        _db.RoleMasters.Add(role);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Role created Id={RoleId} Name={RoleName}", role.Id, role.RoleName);
        await _audit.LogAsync("UserMgmt", "RoleCreated", recordId: role.Id.ToString(), newValue: role.RoleName);
        return CreatedAtAction(nameof(GetRoles), null, role);
    }

    /// <summary>List all roles with their module access.</summary>
    [HttpGet("roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        pageSize = Math.Min(Math.Max(pageSize, 1), 100);
        page = Math.Max(page, 1);

        var query = _db.RoleMasters
            .Include(r => r.ModuleAccess).ThenInclude(ma => ma.Module)
            .Include(r => r.ModuleAccess).ThenInclude(ma => ma.SubModule)
            .AsNoTracking();
        var totalCount = await query.CountAsync();
        var roles = await query
            .OrderBy(r => r.RoleName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
        return Ok(new { data = roles, totalCount, page, pageSize });
    }

    /// <summary>Update an existing role.</summary>
    [HttpPut("roles/{id:int}")]
    [ProducesResponseType(typeof(RoleMaster), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UmCreateRoleDto dto)
    {
        var role = await _db.RoleMasters.FindAsync(id);
        if (role is null) return NotFound();
        role.RoleName = dto.RoleName;
        role.Description = dto.Description;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Role updated Id={RoleId}", role.Id);
        await _audit.LogAsync("UserMgmt", "RoleUpdated", recordId: role.Id.ToString(), newValue: dto.RoleName);
        return Ok(role);
    }

    /// <summary>Deactivate (soft-delete) a role.</summary>
    [HttpDelete("roles/{id:int}")]
    [ProducesResponseType(typeof(RoleMaster), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateRole(int id)
    {
        var role = await _db.RoleMasters.FindAsync(id);
        if (role is null) return NotFound();
        role.IsActive = false;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Role deactivated Id={RoleId}", role.Id);
        await _audit.LogAsync("UserMgmt", "RoleDeactivated", recordId: role.Id.ToString());
        return Ok(role);
    }

    // -------------------------------------------------------------------------
    // Access Matrix
    // -------------------------------------------------------------------------

    /// <summary>Get the full role × module permission matrix.</summary>
    [HttpGet("access-matrix")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccessMatrix(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        pageSize = Math.Min(Math.Max(pageSize, 1), 100);
        page = Math.Max(page, 1);

        var query = _db.RoleModuleAccesses
            .Include(r => r.Role)
            .Include(r => r.Module)
            .Include(r => r.SubModule)
            .AsNoTracking();
        var totalCount = await query.CountAsync();
        var matrix = await query
            .OrderBy(r => r.Role.RoleName)
            .ThenBy(r => r.Module.ModuleName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
        return Ok(new { data = matrix, totalCount, page, pageSize });
    }

    /// <summary>Batch-update access matrix permissions.</summary>
    [HttpPut("access-matrix")]
    [ProducesResponseType(typeof(IEnumerable<RoleModuleAccess>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAccessMatrix([FromBody] List<UmAccessMatrixEntryDto> entries)
    {
        // Load all potentially affected records in a single query
        var roleIds = entries.Select(e => e.RoleId).Distinct().ToList();
        var moduleIds = entries.Select(e => e.ModuleId).Distinct().ToList();
        var allExisting = await _db.RoleModuleAccesses
            .Where(r => roleIds.Contains(r.RoleId) && moduleIds.Contains(r.ModuleId))
            .ToListAsync();

        var saved = new List<RoleModuleAccess>();

        foreach (var entry in entries)
        {
            var existing = allExisting
                .Where(r => r.RoleId == entry.RoleId && r.ModuleId == entry.ModuleId && r.SubModuleId == entry.SubModuleId)
                .ToList();
            _db.RoleModuleAccesses.RemoveRange(existing);

            var access = new RoleModuleAccess
            {
                RoleId = entry.RoleId,
                ModuleId = entry.ModuleId,
                SubModuleId = entry.SubModuleId,
                CanView = entry.CanView,
                CanExecute = entry.CanExecute,
                CanApprove = entry.CanApprove,
                CanDownload = entry.CanDownload,
                CanUpload = entry.CanUpload,
                CanAdmin = entry.CanAdmin
            };
            _db.RoleModuleAccesses.Add(access);
            saved.Add(access);
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Access matrix updated, {Count} entries", saved.Count);
        await _audit.LogAsync("UserMgmt", "PermissionsUpdated", newValue: $"{saved.Count} entries updated");
        return Ok(saved);
    }

    /// <summary>Get effective permissions for a user through their assigned roles.</summary>
    [HttpGet("access/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserAccess(int userId)
    {
        var user = await _db.UserMasters
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return NotFound();

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();

        var permissions = await _db.RoleModuleAccesses
            .Include(r => r.Role)
            .Include(r => r.Module)
            .Include(r => r.SubModule)
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.RoleId))
            .ToListAsync();

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            Roles = user.UserRoles.Select(ur => ur.RoleId),
            Permissions = permissions
        });
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Hash a password using PBKDF2 (same algorithm as AuthController).</summary>
    private static string HashPassword(string password) =>
        AuthController.HashPassword(password);
}

// DTO records for UserMgmt endpoints
public record UmCreateUserDto(
    string FullName,
    string Email,
    string? Mobile,
    string? EmployeeId,
    string? Department,
    string? Password,
    string? Status,
    int? ClientId,
    List<int>? RoleIds);

public record UmUpdateUserDto(
    string? FullName,
    string? Email,
    string? Mobile,
    string? EmployeeId,
    string? Department,
    string? Status,
    int? ClientId,
    List<int>? RoleIds);

public record UmResetPasswordDto(string NewPassword);

public record UmCreateRoleDto(string RoleName, string? Description);

public record UmAccessMatrixEntryDto(
    int RoleId,
    int ModuleId,
    int? SubModuleId,
    bool CanView,
    bool CanExecute,
    bool CanApprove,
    bool CanDownload,
    bool CanUpload,
    bool CanAdmin);
