namespace InsuranceEngine.Api.Models;

/// <summary>Loyalty Income percentage per PPT and policy year.</summary>
public class LoyaltyFactor
{
    public int Id { get; set; }
    public int Ppt { get; set; }
    /// <summary>Minimum policy year (inclusive) for this rate to apply.</summary>
    public int PolicyYearFrom { get; set; }
    /// <summary>Maximum policy year (inclusive); null means no upper limit.</summary>
    public int? PolicyYearTo { get; set; }
    public decimal RatePercent { get; set; }
}
