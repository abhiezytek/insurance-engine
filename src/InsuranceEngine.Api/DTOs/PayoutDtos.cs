namespace InsuranceEngine.Api.DTOs;

public class PayoutCaseDto
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Uin { get; set; } = string.Empty;
    public string PayoutType { get; set; } = string.Empty;
    public string InputMode { get; set; } = string.Empty;
    public int? BatchId { get; set; }
    public decimal CoreSystemAmount { get; set; }
    public decimal PrecisionProAmount { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePct { get; set; }
    public string Status { get; set; } = "Pending";
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
    public DateTime CreatedAt { get; set; }
    public List<PayoutWorkflowStepDto> WorkflowHistory { get; set; } = new();
}

public class PayoutWorkflowStepDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
}

public class PayoutBatchDto
{
    public int Id { get; set; }
    public string BatchType { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string PayoutType { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public int MatchCount { get; set; }
    public int MismatchCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PayoutFileDto
{
    public int Id { get; set; }
    public int? BatchId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileFormat { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int RecordCount { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

public class PayoutDashboardDto
{
    public int TotalThisMonth { get; set; }
    public int PendingCount { get; set; }
    public int CheckerApprovedCount { get; set; }
    public int AuthorizedCount { get; set; }
    public int RejectedCount { get; set; }
    public int MatchCount { get; set; }
    public int MismatchCount { get; set; }
    public decimal TotalVariance { get; set; }
}

// ─── Request DTOs ────────────────────────────────────────────────────────────

public class PayoutSearchRequest
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string PayoutType { get; set; } = "Maturity";
}

public class PayoutDecisionRequest
{
    public int CaseId { get; set; }
    public string? Remarks { get; set; }
}

public class PayoutBulkDecisionRequest
{
    public List<int> CaseIds { get; set; } = new();
    public string? Remarks { get; set; }
}

public class PayoutBatchGenerateRequest
{
    public string PayoutType { get; set; } = "Maturity";
    public DateTime? MaturityDateFrom { get; set; }
    public DateTime? MaturityDateTo { get; set; }
    public int MaxRecords { get; set; } = 100;
}
