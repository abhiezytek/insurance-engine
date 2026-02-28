namespace InsuranceEngine.Api.Models;

/// <summary>Deferred Income GI percentage per PPT, PT, and policy year.</summary>
public class DeferredIncomeFactor
{
    public int Id { get; set; }
    public int Ppt { get; set; }
    public int Pt { get; set; }
    public int PolicyYear { get; set; }
    public decimal RatePercent { get; set; }
}
