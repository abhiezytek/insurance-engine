using System.ComponentModel.DataAnnotations;

namespace InsuranceEngine.Api.Models;

/// <summary>
/// ULIP fund master with FMC rates (effective-dated) for e-Wealth Royale.
/// </summary>
public class UlipFundFmc
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string ProductCode { get; set; } = string.Empty;

    [MaxLength(50)]
    public string FundCode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string FundName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Sfin { get; set; } = string.Empty;

    /// <summary>Annual FMC rate in percent (e.g., 1.35 = 1.35%).</summary>
    public decimal FmcRate { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;
}
