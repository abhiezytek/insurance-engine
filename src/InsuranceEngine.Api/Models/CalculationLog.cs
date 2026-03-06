namespace InsuranceEngine.Api.Models;

/// <summary>Log of calculations performed, for the dashboard activity table.</summary>
public class CalculationLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string? PolicyNumber { get; set; }
    public string InputJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string RequestedBy { get; set; } = "System";
    public string Status { get; set; } = "Completed";
}
