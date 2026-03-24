namespace InsuranceEngine.Api.Models;

public class PayoutFile
{
    public int Id { get; set; }
    public int? BatchId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileFormat { get; set; } = string.Empty;           // "CSV", "JSON"
    public string FileType { get; set; } = string.Empty;             // "Export", "Upload"
    public long FileSizeBytes { get; set; }
    public int RecordCount { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string? StoragePath { get; set; }

    public PayoutBatch? Batch { get; set; }
}
