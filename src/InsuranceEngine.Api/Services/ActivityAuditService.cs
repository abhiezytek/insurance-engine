using System.Security.Claims;
using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.Models;

namespace InsuranceEngine.Api.Services;

/// <summary>
/// Records cross-cutting audit log entries for user actions across all modules.
/// Uses IHttpContextAccessor to automatically capture the current user and IP address.
/// </summary>
public class ActivityAuditService : IActivityAuditService
{
    private readonly InsuranceDbContext _db;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<ActivityAuditService> _logger;

    public ActivityAuditService(
        InsuranceDbContext db,
        IHttpContextAccessor httpContext,
        ILogger<ActivityAuditService> logger)
    {
        _db = db;
        _httpContext = httpContext;
        _logger = logger;
    }

    public async Task LogAsync(
        string module,
        string action,
        string? recordId = null,
        string? oldValue = null,
        string? newValue = null,
        string status = "Success",
        string? errorMessage = null)
    {
        try
        {
            var user = _httpContext.HttpContext?.User;
            var username = user?.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";
            var ip = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString();

            var entry = new AuditLogEntry
            {
                Module = module,
                Action = action,
                EventType = $"{module}.{action}",
                RecordId = recordId,
                OldValue = TruncateForStorage(oldValue),
                NewValue = TruncateForStorage(newValue),
                DoneBy = username,
                IpAddress = ip,
                Status = status,
                DoneAt = DateTime.UtcNow
            };

            _db.AuditLogEntries.Add(entry);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Audit logging must never break the main flow
            _logger.LogError(ex, "Failed to write audit log: Module={Module} Action={Action}", module, action);
        }
    }

    /// <summary>Truncate large values to prevent DB overflow (max 4000 chars for NVARCHAR).</summary>
    private static string? TruncateForStorage(string? value) =>
        value is { Length: > 4000 } ? value[..4000] : value;
}
