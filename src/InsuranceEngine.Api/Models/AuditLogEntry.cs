namespace InsuranceEngine.Api.Models;

public class AuditLogEntry
{
    public int Id { get; set; }
    public int? AuditCaseId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string DoneBy { get; set; } = string.Empty;
    public DateTime DoneAt { get; set; } = DateTime.UtcNow;
}
