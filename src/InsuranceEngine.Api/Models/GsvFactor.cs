namespace InsuranceEngine.Api.Models;

/// <summary>GSV factor from Annexure-2. Percentage of total premiums paid.</summary>
public class GsvFactor
{
    public int Id { get; set; }
    public int Ppt { get; set; }
    public int PolicyYear { get; set; }
    public decimal FactorPercent { get; set; }
}
