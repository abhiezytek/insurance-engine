namespace InsuranceEngine.Api.Models;

/// <summary>Persisted ULIP Benefit Illustration result row (one row per policy year).</summary>
public class UlipIllustrationResult
{
    public int Id { get; set; }

    /// <summary>Policy number used to group all years of one illustration.</summary>
    public string PolicyNumber { get; set; } = string.Empty;

    public int Year { get; set; }

    public int Age { get; set; }

    public decimal Premium { get; set; }

    public decimal PremiumInvested { get; set; }

    public decimal MortalityCharge { get; set; }

    public decimal PolicyCharge { get; set; }

    /// <summary>Fund Value at 4% assumed return.</summary>
    public decimal FundValue4 { get; set; }

    /// <summary>Fund Value at 8% assumed return.</summary>
    public decimal FundValue8 { get; set; }

    /// <summary>Death Benefit at 4% scenario.</summary>
    public decimal DeathBenefit4 { get; set; }

    /// <summary>Death Benefit at 8% scenario.</summary>
    public decimal DeathBenefit8 { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
