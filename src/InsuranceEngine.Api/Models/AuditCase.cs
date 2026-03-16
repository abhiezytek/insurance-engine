namespace InsuranceEngine.Api.Models;

public class AuditCase
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Uin { get; set; } = string.Empty;
    public string AuditType { get; set; } = string.Empty;
    public string InputMode { get; set; } = string.Empty;
    public int? BatchId { get; set; }
    public decimal CoreSystemAmount { get; set; }
    public decimal PrecisionProAmount { get; set; }
    public decimal Variance { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime? PolicyAnniversary { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AuditBatch? Batch { get; set; }
    public List<AuditDecision> Decisions { get; set; } = new();
}
