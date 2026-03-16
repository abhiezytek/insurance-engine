namespace InsuranceEngine.Api.DTOs;

public class AuditCaseResultDto
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Uin { get; set; } = string.Empty;
    public DateTime? PolicyAnniversary { get; set; }
    public string AuditType { get; set; } = string.Empty;
    public string InputMode { get; set; } = string.Empty;
    public decimal CoreSystemAmount { get; set; }
    public decimal PrecisionProAmount { get; set; }
    public decimal Variance { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Remarks { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AuditDecisionRequest
{
    public int CaseId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class BulkDecisionRequest
{
    public List<int> CaseIds { get; set; } = new();
    public string Decision { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class AuditDashboardDto
{
    public int TotalThisMonth { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int PendingCount { get; set; }
    public decimal TotalVariance { get; set; }
}

public class AuditBatchDto
{
    public int Id { get; set; }
    public DateTime UploadDate { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string AuditType { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public int PendingCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
}

public class AuditLogDto
{
    public int Id { get; set; }
    public int? AuditCaseId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string DoneBy { get; set; } = string.Empty;
    public DateTime DoneAt { get; set; }
}

public class SinglePolicySearchRequest
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string AuditType { get; set; } = "PayoutVerification";
}
