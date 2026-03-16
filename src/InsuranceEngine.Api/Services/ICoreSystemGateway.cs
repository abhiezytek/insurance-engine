namespace InsuranceEngine.Api.Services;

public class PolicyMasterDto
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Uin { get; set; } = string.Empty;
    public DateTime? PolicyAnniversary { get; set; }
    public decimal AnnualPremium { get; set; }
    public int PolicyTerm { get; set; }
    public int PremiumPayingTerm { get; set; }
    public int PremiumsPaid { get; set; }
    public int EntryAge { get; set; }
    public string Option { get; set; } = "Immediate";
    public string Channel { get; set; } = "Other";
    public string ProductCategory { get; set; } = "Traditional";
    public string PolicyStatus { get; set; } = "InForce";
}

public class CoreAmountDto
{
    public decimal Amount { get; set; }
    public DateTime? CalculationDate { get; set; }
    public string SourceReference { get; set; } = string.Empty;
}

public class PushResponseDto
{
    public bool Success { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface ICoreSystemGateway
{
    Task<PolicyMasterDto?> FetchPolicyByPolicyNumber(string policyNumber);
    Task<CoreAmountDto> FetchCoreSystemAmount(string policyNumber, string auditType);
    Task<PushResponseDto> PushApprovalToCore(string policyNumber, string auditType, decimal approvedAmount, string? remarks, string userId);
}
