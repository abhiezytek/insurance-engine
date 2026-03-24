namespace InsuranceEngine.Api.Models;

public class PayoutWorkflowHistory
{
    public int Id { get; set; }
    public int PayoutCaseId { get; set; }
    public string Action { get; set; } = string.Empty;               // "Created", "CheckerApproved", "CheckerRejected", "Authorized", "Rejected", "PushedToCore"
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string? PushStatus { get; set; }                          // "Success", "Failed" (only for PushedToCore action)
    public string? PushReferenceNumber { get; set; }
    public string? PushErrorMessage { get; set; }

    public PayoutCase PayoutCase { get; set; } = null!;
}
