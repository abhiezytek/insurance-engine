namespace InsuranceEngine.Api.Models;

/// <summary>Configurable charge structure for ULIP products.</summary>
public class UlipCharge
{
    public int Id { get; set; }

    /// <summary>Reference to the ULIP product (maps to Product.Id).</summary>
    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    /// <summary>
    /// Type of charge:
    /// PremiumAllocation, PolicyAdmin, FMC, MortalityBase.
    /// </summary>
    public string ChargeType { get; set; } = string.Empty;

    /// <summary>Charge value (percentage or fixed amount depending on ChargeFrequency).</summary>
    public decimal ChargeValue { get; set; }

    /// <summary>
    /// Charge frequency / unit:
    /// PercentOfPremium, PercentOfFund, MonthlyFixed, Annual.
    /// </summary>
    public string ChargeFrequency { get; set; } = string.Empty;

    /// <summary>Policy year to which this charge applies (null = all years).</summary>
    public int? PolicyYear { get; set; }
}
