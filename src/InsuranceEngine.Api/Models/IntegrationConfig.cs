namespace InsuranceEngine.Api.Models;

public class IntegrationConfig
{
    public int Id { get; set; }
    public string ConfigName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string? AuthType { get; set; }
    public string? AuthToken { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public bool IsMock { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
