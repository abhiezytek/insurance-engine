namespace InsuranceEngine.Api.Exceptions;

/// <summary>
/// Thrown when product configuration data (factor tables, rule CSVs, seed data)
/// is missing or malformed. Maps to HTTP 500 — this is an internal setup issue,
/// not a client error.
/// </summary>
public class ProductConfigurationException : Exception
{
    public ProductConfigurationException(string message) : base(message) { }
    public ProductConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when the caller's request violates product business rules
/// (e.g. invalid PPT/PT combination, allocation below minimum, premium below threshold).
/// Maps to HTTP 400 (Bad Request).
/// </summary>
public class ProductValidationException : Exception
{
    public ProductValidationException(string message) : base(message) { }
    public ProductValidationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a required product rule or factor lookup fails.
/// Maps to HTTP 400 when the request parameters are unsupported,
/// or HTTP 500 when the root cause is missing internal config.
/// Convention: if the caller can fix the request, use <see cref="ProductValidationException"/> instead.
/// This exception is for cases where a valid-looking request hits a gap in factor/rule data.
/// </summary>
public class ProductRuleNotFoundException : Exception
{
    /// <summary>
    /// When true, the missing rule is caused by incomplete internal configuration
    /// (maps to 500). When false, the client requested an unsupported combination (maps to 400).
    /// </summary>
    public bool IsConfigGap { get; }

    public ProductRuleNotFoundException(string message, bool isConfigGap = false)
        : base(message)
    {
        IsConfigGap = isConfigGap;
    }

    public ProductRuleNotFoundException(string message, bool isConfigGap, Exception innerException)
        : base(message, innerException)
    {
        IsConfigGap = isConfigGap;
    }
}
