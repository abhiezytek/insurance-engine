namespace InsuranceEngine.Api.Models;

public class ExcelUploadRowError
{
    public int Id { get; set; }
    public int ExcelUploadBatchId { get; set; }
    public ExcelUploadBatch ExcelUploadBatch { get; set; } = null!;
    public int RowNumber { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string? RowData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
