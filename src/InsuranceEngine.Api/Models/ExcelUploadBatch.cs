namespace InsuranceEngine.Api.Models;

public class ExcelUploadBatch
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string UploadType { get; set; } = string.Empty; // Products, Parameters, Formulas, Rules
    public string Status { get; set; } = "Processing"; // Processing, Completed, Failed
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int ErrorRows { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public ICollection<ExcelUploadRowError> RowErrors { get; set; } = new List<ExcelUploadRowError>();
}
