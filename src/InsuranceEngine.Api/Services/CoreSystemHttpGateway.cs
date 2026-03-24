namespace InsuranceEngine.Api.Services;

/// <summary>Configuration for the core system HTTP API integration.</summary>
public class CoreSystemConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 30;
    public bool UseMock { get; set; } = true;
}

/// <summary>
/// HTTP-based core system gateway for production use.
/// Replace the mock with real API calls when the core system is available.
/// </summary>
public class CoreSystemHttpGateway : ICoreSystemGateway
{
    private readonly HttpClient _httpClient;
    private readonly CoreSystemConfig _config;
    private readonly ILogger<CoreSystemHttpGateway> _logger;

    public CoreSystemHttpGateway(
        HttpClient httpClient,
        Microsoft.Extensions.Options.IOptions<CoreSystemConfig> config,
        ILogger<CoreSystemHttpGateway> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_config.BaseUrl))
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);

        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        if (!string.IsNullOrWhiteSpace(_config.ApiKey))
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _config.ApiKey);
    }

    public async Task<PolicyMasterDto?> FetchPolicyByPolicyNumber(string policyNumber)
    {
        _logger.LogInformation("CoreSystemHttpGateway: FetchPolicyByPolicyNumber for {PolicyNumber}", policyNumber);

        // When core system API is ready, uncomment:
        // var response = await _httpClient.GetAsync($"api/policy/{policyNumber}");
        // if (!response.IsSuccessStatusCode) return null;
        // return await response.Content.ReadFromJsonAsync<PolicyMasterDto>();

        return null; // Stub: returns null to indicate "not found" until API is wired
    }

    public async Task<CoreAmountDto> FetchCoreSystemAmount(string policyNumber, string auditType)
    {
        _logger.LogInformation("CoreSystemHttpGateway: FetchCoreSystemAmount for {PolicyNumber}, type={AuditType}", policyNumber, auditType);

        // When core system API is ready, uncomment:
        // var response = await _httpClient.GetAsync(
        //     $"api/policy/{policyNumber}/payout?type={auditType}&asOn={DateTime.UtcNow:yyyy-MM-dd}");
        // response.EnsureSuccessStatusCode();
        // return await response.Content.ReadFromJsonAsync<CoreAmountDto>()
        //     ?? new CoreAmountDto();

        await Task.CompletedTask;
        return new CoreAmountDto
        {
            Amount = 0m,
            CalculationDate = DateTime.UtcNow,
            SourceReference = "CORE-API-STUB"
        };
    }

    public async Task<PushResponseDto> PushApprovalToCore(string policyNumber, string auditType, decimal approvedAmount, string? remarks, string userId)
    {
        _logger.LogInformation(
            "CoreSystemHttpGateway: PushApprovalToCore for {PolicyNumber}, type={AuditType}, amount={Amount}",
            policyNumber, auditType, approvedAmount);

        // When core system API is ready, uncomment:
        // var payload = new { policyNumber, auditType, approvedAmount, remarks, userId };
        // var response = await _httpClient.PostAsJsonAsync("api/payout/confirm", payload);
        // return await response.Content.ReadFromJsonAsync<PushResponseDto>()
        //     ?? new PushResponseDto { Success = false, ErrorMessage = "Null response" };

        await Task.CompletedTask;
        return new PushResponseDto
        {
            Success = false,
            ErrorMessage = "Core system API not configured"
        };
    }
}
