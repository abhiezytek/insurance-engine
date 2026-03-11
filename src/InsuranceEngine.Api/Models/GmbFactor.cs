namespace InsuranceEngine.Api.Models;

/// <summary>GMB factor from Annexure-1. Keyed by PPT, PT, EntryAgeMin, EntryAgeMax, and Option.</summary>
public class GmbFactor
{
    public int Id { get; set; }
    public int Ppt { get; set; }
    public int Pt { get; set; }
    public int EntryAgeMin { get; set; }
    public int EntryAgeMax { get; set; }
    /// <summary>Immediate, Deferred, or Twin</summary>
    public string Option { get; set; } = "Immediate";
    public decimal Factor { get; set; }
}
