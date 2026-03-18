using System.ComponentModel.DataAnnotations;

namespace InsuranceEngine.Api.Models;

/// <summary>
/// Configurable income schedule row for Century Income (GI/LI/Twin) sourced from F-and-U / BI.
/// </summary>
public class IncomeScheduleRate
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Immediate | Deferred | Twin (case-insensitive business values).</summary>
    [MaxLength(50)]
    public string Option { get; set; } = string.Empty;

    /// <summary>GI or LI.</summary>
    [MaxLength(20)]
    public string BenefitType { get; set; } = string.Empty;

    public int Ppt { get; set; }
    public int Pt { get; set; }
    public int PolicyYear { get; set; }

    /// <summary>Percent-of-Annualized-Premium, Percent-of-Sum-Assured, etc.</summary>
    [MaxLength(50)]
    public string RateType { get; set; } = "PERCENT_OF_ANNUALIZED_PREMIUM";

    /// <summary>Rate expressed in the unit implied by RateType (e.g., 10 = 10%).</summary>
    public decimal Rate { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
