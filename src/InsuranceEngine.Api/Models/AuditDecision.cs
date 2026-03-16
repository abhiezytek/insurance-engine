namespace InsuranceEngine.Api.Models;

public class AuditDecision
{
    public int Id { get; set; }
    public int AuditCaseId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string DecidedBy { get; set; } = string.Empty;
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
    public bool PushedToCore { get; set; } = false;
    public string? PushStatus { get; set; }
    public DateTime? PushTimestamp { get; set; }
    public string? PushErrorMessage { get; set; }

    public AuditCase AuditCase { get; set; } = null!;
}
