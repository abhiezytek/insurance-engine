using System.ComponentModel.DataAnnotations;

namespace InsuranceEngine.Api.Models;

/// <summary>
/// Projection configuration (e.g., as-on-date maturity forward projection rate and assumptions).
/// </summary>
public class ProjectionConfig
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string TemplateCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ProjectionMethod { get; set; } = "FORWARD_FROM_AS_ON_DATE";

    public bool AssumeFuturePremiumsPaid { get; set; } = true;

    /// <summary>Default projection rate (e.g., 0.04 = 4%).</summary>
    public decimal DefaultProjectionRate { get; set; } = 0.04m;

    [MaxLength(500)]
    public string? AlternateProjectionRateNote { get; set; }

    public bool RequiresBusinessConfirmation { get; set; } = true;
}
