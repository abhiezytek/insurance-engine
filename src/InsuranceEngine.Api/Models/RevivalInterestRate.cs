using System.ComponentModel.DataAnnotations;

namespace InsuranceEngine.Api.Models;

/// <summary>
/// Effective-dated revival interest configuration (e.g., 10Y G-Sec + 150 bps, rounded to next 25 bps).
/// </summary>
public class RevivalInterestRate
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Optional key to differentiate multiple rate bases (e.g., CENTURY_INCOME_REVIVAL_INTEREST).</summary>
    [MaxLength(100)]
    public string RateKey { get; set; } = string.Empty;

    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }

    /// <summary>Annualized rate, e.g., 8.75 = 8.75%.</summary>
    public decimal AnnualRate { get; set; }

    /// <summary>Compounding convention, e.g., HALF_YEARLY.</summary>
    [MaxLength(50)]
    public string Compounding { get; set; } = "HALF_YEARLY";

    [MaxLength(500)]
    public string BasisDescription { get; set; } = string.Empty;

    public DateTime? SourceDate { get; set; }
}
