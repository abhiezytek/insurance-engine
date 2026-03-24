namespace InsuranceEngine.Api.Services;

/// <summary>
/// Cross-cutting activity audit service for logging user actions across all modules.
/// Distinct from IAuditService which handles the Audit module's payout verification workflow.
/// </summary>
public interface IActivityAuditService
{
    /// <summary>Log an auditable user action.</summary>
    Task LogAsync(
        string module,
        string action,
        string? recordId = null,
        string? oldValue = null,
        string? newValue = null,
        string status = "Success",
        string? errorMessage = null);
}
