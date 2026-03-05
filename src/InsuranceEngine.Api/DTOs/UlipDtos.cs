namespace InsuranceEngine.Api.DTOs;

// ---------------------------------------------------------------------------
// Abbreviations (per IRDAI / product spec):
//   AP  = Annualized Premium
//   SA  = Sum Assured
//   PT  = Policy Term
//   PPT = Premium Payment Term
//   FV  = Fund Value
//   NAV = Net Asset Value
//   FMC = Fund Management Charge
//   MC  = Mortality Charge
//   PC  = Policy (Admin) Charge
//   U   = Units
//   AV  = Account Value
//   DB  = Death Benefit
// ---------------------------------------------------------------------------

/// <summary>Fund allocation entry: fund type + percentage.</summary>
public class UlipFundAllocation
{
    /// <summary>Name of the fund (e.g. "Equity Growth", "Debt Fund").</summary>
    public string FundType { get; set; } = string.Empty;

    /// <summary>Allocation percentage (must sum to 100 across all entries).</summary>
    public decimal AllocationPercent { get; set; }
}

/// <summary>Request payload for generating a ULIP Benefit Illustration.</summary>
public class UlipCalculationRequest
{
    /// <summary>Unique policy number (used to persist and retrieve the illustration).</summary>
    public string PolicyNumber { get; set; } = string.Empty;

    /// <summary>Full name of the customer / life assured.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Product code for the ULIP (e.g. "EWEALTH-ROYALE").</summary>
    public string ProductCode { get; set; } = "EWEALTH-ROYALE";

    /// <summary>Gender of the life assured: Male or Female.</summary>
    public string Gender { get; set; } = "Male";

    /// <summary>Date of birth of the life assured.</summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>Entry age (years) of the life assured.</summary>
    public int EntryAge { get; set; }

    /// <summary>Policy Term (PT) in years.</summary>
    public int PolicyTerm { get; set; }

    /// <summary>Premium Payment Term (PPT) in years.</summary>
    public int Ppt { get; set; }

    /// <summary>Annualized Premium (AP) in INR.</summary>
    public decimal AnnualizedPremium { get; set; }

    /// <summary>Sum Assured (SA) in INR.</summary>
    public decimal SumAssured { get; set; }

    /// <summary>
    /// Premium frequency: Yearly, HalfYearly, Quarterly, Monthly.
    /// Used to calculate the installment premium; AP is always the annualized figure.
    /// </summary>
    public string PremiumFrequency { get; set; } = "Yearly";

    /// <summary>Fund allocations — must sum to 100%.</summary>
    public List<UlipFundAllocation> FundAllocations { get; set; } = new();
}

/// <summary>Single policy-year row of the ULIP benefit illustration (dual-scenario).</summary>
public class UlipIllustrationRow
{
    /// <summary>Policy Year.</summary>
    public int Year { get; set; }

    /// <summary>Age of life assured at end of this policy year.</summary>
    public int Age { get; set; }

    /// <summary>Annual premium due this year (AP; 0 after PPT).</summary>
    public decimal AnnualPremium { get; set; }

    /// <summary>Premium invested = AP × (1 − PremiumAllocationCharge%).</summary>
    public decimal PremiumInvested { get; set; }

    /// <summary>Mortality Charge (MC) deducted this year.</summary>
    public decimal MortalityCharge { get; set; }

    /// <summary>Policy Administration Charge (PC) deducted this year.</summary>
    public decimal PolicyCharge { get; set; }

    // ------------------------------------------------------------------
    // 4% assumed return scenario
    // ------------------------------------------------------------------

    /// <summary>Fund Value (FV) at end of year — 4% scenario.</summary>
    public decimal FundValue4 { get; set; }

    /// <summary>Death Benefit (DB) — 4% scenario: max(SA, FV).</summary>
    public decimal DeathBenefit4 { get; set; }

    // ------------------------------------------------------------------
    // 8% assumed return scenario
    // ------------------------------------------------------------------

    /// <summary>Fund Value (FV) at end of year — 8% scenario.</summary>
    public decimal FundValue8 { get; set; }

    /// <summary>Death Benefit (DB) — 8% scenario: max(SA, FV).</summary>
    public decimal DeathBenefit8 { get; set; }
}

/// <summary>Full ULIP Benefit Illustration response including both rate scenarios.</summary>
public class UlipCalculationResponse
{
    // -- Policy inputs echoed back --
    public string PolicyNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int EntryAge { get; set; }
    public int PolicyTerm { get; set; }
    public int Ppt { get; set; }

    /// <summary>Annualized Premium (AP).</summary>
    public decimal AnnualizedPremium { get; set; }

    /// <summary>Sum Assured (SA).</summary>
    public decimal SumAssured { get; set; }

    public string PremiumFrequency { get; set; } = string.Empty;

    // -- Summary values --

    /// <summary>Maturity Benefit at 4% assumed return (= FV at final year, 4% scenario).</summary>
    public decimal MaturityBenefit4 { get; set; }

    /// <summary>Maturity Benefit at 8% assumed return (= FV at final year, 8% scenario).</summary>
    public decimal MaturityBenefit8 { get; set; }

    /// <summary>IRDAI-mandated investment risk disclaimer.</summary>
    public string IrdaiDisclaimer { get; set; } =
        "In this policy, the investment risk in the investment portfolio is borne by the policyholder. " +
        "The linked insurance products do not offer any liquidity during the first five years of the contract. " +
        "The policyholder will not be able to surrender/withdraw the monies invested in linked insurance products " +
        "completely or partially till the end of fifth year. " +
        "Returns shown at 4% p.a. and 8% p.a. are for illustration purposes only and are not guaranteed.";

    /// <summary>Yearly illustration rows — one entry per policy year.</summary>
    public List<UlipIllustrationRow> YearlyTable { get; set; } = new();
}

/// <summary>Request for uploading a mortality rate table via Excel/CSV.</summary>
public class UploadMortalityRequest
{
    public string Gender { get; set; } = "Male";
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
}

/// <summary>Request for uploading ULIP charge structure via Excel/CSV.</summary>
public class UploadChargesRequest
{
    public string ProductCode { get; set; } = string.Empty;
}
