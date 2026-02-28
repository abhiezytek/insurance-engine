namespace InsuranceEngine.Api.Models;

/// <summary>SSV factors from Annexure-3.</summary>
public class SsvFactor
{
    public int Id { get; set; }
    public int Ppt { get; set; }
    public int PolicyYear { get; set; }
    /// <summary>Applied to Paid-Up GMB.</summary>
    public decimal Factor1 { get; set; }
    /// <summary>Applied to income (GI × t/n + Reduced LI).</summary>
    public decimal Factor2 { get; set; }
}
