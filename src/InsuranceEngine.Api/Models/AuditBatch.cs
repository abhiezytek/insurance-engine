namespace InsuranceEngine.Api.Models;

public class AuditBatch
{
    public int Id { get; set; }
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public string FileName { get; set; } = string.Empty;
    public string AuditType { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; } = 0;
    public string Status { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;

    public List<AuditCase> Cases { get; set; } = new();
}
