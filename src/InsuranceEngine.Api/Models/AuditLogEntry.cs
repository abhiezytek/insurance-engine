namespace InsuranceEngine.Api.Models;

public class AuditLogEntry
{
    public int Id { get; set; }
    public int? AuditCaseId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Module { get; set; }
    public string? Action { get; set; }
    public string? RecordId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string DoneBy { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Status { get; set; } = "Success";
    public DateTime DoneAt { get; set; } = DateTime.UtcNow;
}
