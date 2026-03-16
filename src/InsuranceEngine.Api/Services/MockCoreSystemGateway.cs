namespace InsuranceEngine.Api.Services;

public class MockCoreSystemGateway : ICoreSystemGateway
{
    private readonly ILogger<MockCoreSystemGateway> _logger;

    public MockCoreSystemGateway(ILogger<MockCoreSystemGateway> logger)
    {
        _logger = logger;
    }

    public Task<PolicyMasterDto?> FetchPolicyByPolicyNumber(string policyNumber)
    {
        _logger.LogInformation("MockCoreSystemGateway: FetchPolicyByPolicyNumber called for {PolicyNumber}", policyNumber);

        var hash = GetDeterministicHash(policyNumber);
        var isUlip = policyNumber.StartsWith("UL", StringComparison.OrdinalIgnoreCase);

        var dto = new PolicyMasterDto
        {
            PolicyNumber = policyNumber,
            ProductName = isUlip ? "ULIP Growth Fund" : "Endowment Assurance Plan",
            Uin = $"999N{hash % 1000:D3}V01",
            PolicyAnniversary = DateTime.UtcNow.AddMonths(-(hash % 12)),
            AnnualPremium = 50000m + (hash % 10) * 5000m,
            PolicyTerm = 20,
            PremiumPayingTerm = 10,
            PremiumsPaid = 3 + (hash % 8),
            EntryAge = 25 + (hash % 20),
            Option = "Immediate",
            Channel = "Other",
            ProductCategory = isUlip ? "ULIP" : "Traditional",
            PolicyStatus = "InForce"
        };

        return Task.FromResult<PolicyMasterDto?>(dto);
    }

    public Task<CoreAmountDto> FetchCoreSystemAmount(string policyNumber, string auditType)
    {
        _logger.LogInformation("MockCoreSystemGateway: FetchCoreSystemAmount called for {PolicyNumber}, AuditType={AuditType}", policyNumber, auditType);

        var hash = GetDeterministicHash(policyNumber);
        var variance = (hash % 1000) / 100m;

        decimal amount = auditType == "PayoutVerification"
            ? 500000m + variance
            : 25000m + variance;

        var dto = new CoreAmountDto
        {
            Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero),
            CalculationDate = DateTime.UtcNow,
            SourceReference = $"CORE-{policyNumber}-{(auditType.Length >= 3 ? auditType[..3] : auditType).ToUpperInvariant()}"
        };

        return Task.FromResult(dto);
    }

    public Task<PushResponseDto> PushApprovalToCore(string policyNumber, string auditType, decimal approvedAmount, string? remarks, string userId)
    {
        _logger.LogInformation(
            "MockCoreSystemGateway: PushApprovalToCore called for {PolicyNumber}, AuditType={AuditType}, Amount={Amount}, User={UserId}",
            policyNumber, auditType, approvedAmount, userId);

        var refNumber = $"REF-{DateTime.UtcNow:yyyyMMddHHmmss}-{policyNumber}";

        var dto = new PushResponseDto
        {
            Success = true,
            ReferenceNumber = refNumber,
            ErrorMessage = null
        };

        return Task.FromResult(dto);
    }

    private static int GetDeterministicHash(string input)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in input)
                hash = hash * 31 + c;
            return Math.Abs(hash);
        }
    }
}
