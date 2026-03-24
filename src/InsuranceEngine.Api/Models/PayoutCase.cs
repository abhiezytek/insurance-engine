namespace InsuranceEngine.Api.Models;

public class PayoutCase
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Uin { get; set; } = string.Empty;
    public string PayoutType { get; set; } = string.Empty;           // "Maturity", "Surrender", "DeathClaim"
    public string InputMode { get; set; } = string.Empty;            // "Single", "FileUpload", "SystemGenerated"
    public int? BatchId { get; set; }
    public decimal CoreSystemAmount { get; set; }
    public decimal PrecisionProAmount { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePct { get; set; }
    public string Status { get; set; } = "Pending";                  // "Pending", "CheckerApproved", "CheckerRejected", "Authorized", "Rejected"
    public string? Remarks { get; set; }
    public DateTime? PolicyStartDate { get; set; }
    public DateTime? PolicyMaturityDate { get; set; }
    public decimal? SumAssured { get; set; }
    public decimal? AnnualPremium { get; set; }
    public int? PolicyTerm { get; set; }
    public int? PremiumPayingTerm { get; set; }
    public string? ProductVersion { get; set; }
    public string? FactorVersion { get; set; }
    public string? FormulaVersion { get; set; }
    public string? CalculationSource { get; set; }
    public DateTime? CalculatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PayoutBatch? Batch { get; set; }
    public List<PayoutWorkflowHistory> WorkflowHistory { get; set; } = new();
}
