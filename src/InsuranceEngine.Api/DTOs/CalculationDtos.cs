namespace InsuranceEngine.Api.DTOs;

/// <summary>Request body for a traditional product calculation.</summary>
public class TraditionalCalculationRequest
{
    /// <summary>Unique product code, e.g. <c>CENTURY_INCOME</c>.</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Specific version string (e.g. <c>1.0</c>). Omit to use the latest active version.</summary>
    public string? Version { get; set; }

    /// <summary>Input parameter values keyed by parameter name (e.g. AP, SA, Age).</summary>
    public Dictionary<string, decimal> Parameters { get; set; } = new();
}

/// <summary>Calculated formula results for a product version.</summary>
public class TraditionalCalculationResponse
{
    /// <summary>Product code used for the calculation.</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Version of the product used.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Computed formula values keyed by formula name (e.g. GMB, DEATH_BENEFIT).</summary>
    public Dictionary<string, decimal> Results { get; set; } = new();
}

/// <summary>Request body for ad-hoc formula testing.</summary>
public class FormulaTestRequest
{
    /// <summary>Expression to evaluate. Leave empty to use the stored formula expression.</summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>Parameter values to substitute into the expression.</summary>
    public Dictionary<string, decimal> Parameters { get; set; } = new();
}

/// <summary>Result of an ad-hoc formula test.</summary>
public class FormulaTestResponse
{
    /// <summary>Computed numeric result (only meaningful when <see cref="Success"/> is <c>true</c>).</summary>
    public decimal Result { get; set; }

    /// <summary>Whether evaluation succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Error message if evaluation failed; otherwise <c>null</c>.</summary>
    public string? Error { get; set; }
}
