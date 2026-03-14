namespace InsuranceEngine.Api.Models;

/// <summary>SSV factors from Annexure-3, keyed by (PPT, PT, Option, PolicyYear).</summary>
public class SsvFactor
{
    public int Id { get; set; }
    public int Ppt { get; set; }
    public int Pt { get; set; }
    /// <summary>Income option: Immediate, Deferred, or Twin.</summary>
    public string Option { get; set; } = string.Empty;
    public int PolicyYear { get; set; }
    /// <summary>Applied to Paid-Up GMB.</summary>
    public decimal Factor1 { get; set; }
    /// <summary>Applied to income (GI × t/n + Reduced LI).</summary>
    public decimal Factor2 { get; set; }
}
