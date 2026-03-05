namespace InsuranceEngine.Api.Models;

/// <summary>Age-wise mortality rate table for ULIP mortality charge calculations.</summary>
public class MortalityRate
{
    public int Id { get; set; }

    /// <summary>Age of the life assured.</summary>
    public int Age { get; set; }

    /// <summary>Gender: Male or Female.</summary>
    public string Gender { get; set; } = "Male";

    /// <summary>Mortality rate per 1000 sum at risk.</summary>
    public decimal Rate { get; set; }

    /// <summary>Date from which this rate is effective.</summary>
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
}
