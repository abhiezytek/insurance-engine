namespace InsuranceEngine.Api.DTOs;

public class TraditionalCalculationRequest
{
    public string ProductCode { get; set; } = string.Empty;
    public string? Version { get; set; }
    public Dictionary<string, decimal> Parameters { get; set; } = new();
}

public class TraditionalCalculationResponse
{
    public string ProductCode { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, decimal> Results { get; set; } = new();
}

public class FormulaTestRequest
{
    public string Expression { get; set; } = string.Empty;
    public Dictionary<string, decimal> Parameters { get; set; } = new();
}

public class FormulaTestResponse
{
    public decimal Result { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
