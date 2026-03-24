namespace InsuranceEngine.Api.Models;

public class PayoutBatch
{
    public int Id { get; set; }
    public string BatchType { get; set; } = string.Empty;            // "FileUpload", "SystemGenerated"
    public string? FileName { get; set; }
    public string PayoutType { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public int MatchCount { get; set; }
    public int MismatchCount { get; set; }
    public string Status { get; set; } = "Processing";               // "Processing", "Processed", "PartiallyProcessed"
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<PayoutCase> Cases { get; set; } = new();
}
