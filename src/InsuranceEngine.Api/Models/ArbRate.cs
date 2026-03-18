using System.ComponentModel.DataAnnotations;

namespace InsuranceEngine.Api.Models;

/// <summary>
/// Additional Risk Benefit (ARB) annual rate per 1000 SAR, effective-dated and dimensional.
/// </summary>
public class ArbRate
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string ProductCode { get; set; } = string.Empty;

    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }

    public int? AgeMin { get; set; }
    public int? AgeMax { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(50)]
    public string? UwClass { get; set; }

    [MaxLength(20)]
    public string? SmokerStatus { get; set; }

    /// <summary>Rate per 1000 SAR (e.g., 1.25 = ₹1.25 per 1000 per annum).</summary>
    public decimal AnnualRatePer1000Sar { get; set; }

    [MaxLength(100)]
    public string? SourceAnnexure { get; set; }

    [MaxLength(50)]
    public string? SourceVersion { get; set; }
}
