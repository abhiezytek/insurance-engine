namespace InsuranceEngine.Api.DTOs;

// ---------------------------------------------------------------------------
// Abbreviations (per IRDAI / SUD Life e-Wealth Royale product spec):
//   AP   = Annualized Premium
//   SA   = Sum Assured
//   SAR  = Sum At Risk  (max(SA - FV - partial_withdrawals_last_24m, 105%×TPP - FV, 0))
//   PT   = Policy Term
//   PPT  = Premium Payment Term
//   FV   = Fund Value
//   FMC  = Fund Management Charge (0.1118% per month for Self-Managed strategy)
//   MC   = Mortality Charge  (SAR × annual_rate / 12000 per month)
//   PAC  = Policy Administration Charge (₹100/month for first 10 years)
//   LA   = Loyalty Addition (0.10% × avg 12-month FV, from end-of-year 6 to end-of-PPT)
//   WB   = Wealth Booster  (3% × avg 24-month FV, at end of years 10, 15, 20, ...)
//   RoPAC= Return of Policy Admin Charges (at end of year 10)
//   RoMC = Return of Mortality Charges (at maturity)
//   DB   = Death Benefit = max(SA, FV, 105% × total premiums paid)
//   TPP  = Total Premiums Paid (cumulative AP paid to date)
//   DC   = Discontinuance Charge (IRDAI-mandated, years 1-4 only)
// ---------------------------------------------------------------------------

/// <summary>Fund allocation entry: fund type + percentage.</summary>
public class UlipFundAllocation
{
    /// <summary>Name of the fund (e.g. "SUD Life Nifty Alpha 50 Index Fund").</summary>
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

    /// <summary>Full name of the policyholder (if different from life assured).</summary>
    public string PolicyholderName { get; set; } = string.Empty;

    /// <summary>Whether Life Assured is the same as Policyholder.</summary>
    public bool LifeAssuredSameAsPolicyholder { get; set; } = true;

    /// <summary>Product code for the ULIP (e.g. "EWEALTH-ROYALE").</summary>
    public string ProductCode { get; set; } = "EWEALTH-ROYALE";

    /// <summary>Plan option: Platinum or Platinum Plus.</summary>
    public string Option { get; set; } = "Platinum";

    // ---- Life Assured details ----

    /// <summary>Gender of the life assured: Male or Female.</summary>
    public string Gender { get; set; } = "Male";

    /// <summary>Date of birth of the life assured.</summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>Entry age (years last birthday) of the life assured.</summary>
    public int EntryAge { get; set; }

    // ---- Policyholder details ----

    /// <summary>Date of birth of the policyholder.</summary>
    public DateTime PolicyholderDateOfBirth { get; set; }

    /// <summary>Age of the policyholder (years last birthday).</summary>
    public int PolicyholderAge { get; set; }

    /// <summary>Gender of the policyholder: Male or Female.</summary>
    public string PolicyholderGender { get; set; } = "Male";

    // ---- Policy parameters ----

    /// <summary>Policy Term (PT) in years.</summary>
    public int PolicyTerm { get; set; }

    /// <summary>Premium Payment Term (PPT) in years.</summary>
    public int Ppt { get; set; }

    /// <summary>Annualized Premium (AP) in INR (excl. GST, riders, underwriting extra).</summary>
    public decimal AnnualizedPremium { get; set; }

    /// <summary>Sum Assured (SA) in INR.</summary>
    public decimal SumAssured { get; set; }

    /// <summary>
    /// Premium frequency: Yearly, Half Yearly, Quarterly, Monthly.
    /// AP is always the annualized figure; installment = AP × frequency_factor.
    /// </summary>
    public string PremiumFrequency { get; set; } = "Yearly";

    /// <summary>
    /// Policy effective date (commencement date). Used to compute month-of-birthday
    /// transitions for monthly mortality rate application.
    /// Defaults to UTC today if not set.
    /// </summary>
    public DateTime? PolicyEffectiveDate { get; set; }

    // ---- PPT type ----

    /// <summary>Type of Premium Payment Term: "Limited", "Single", or "Till_Maturity".</summary>
    public string TypeOfPpt { get; set; } = "Limited";

    // ---- Investment strategy ----

    /// <summary>Fund option category: Self-Managed, Target_Maturity, Structured.</summary>
    public string FundOption { get; set; } = string.Empty;

    /// <summary>Investment strategy: Age-based Investment Strategy or Self-Managed Investment Strategy.</summary>
    public string InvestmentStrategy { get; set; } = "Self-Managed Investment Strategy";

    /// <summary>Risk preference (required only for Age-based Investment Strategy): Aggressive or Conservative.</summary>
    public string? RiskPreference { get; set; }

    /// <summary>Fund allocations — must sum to 100% for Self-Managed strategy.</summary>
    public List<UlipFundAllocation> FundAllocations { get; set; } = new();

    // ---- Distribution / underwriting ----

    /// <summary>Distribution channel (e.g. Corporate Agency, Broker, Direct Marketing).</summary>
    public string DistributionChannel { get; set; } = string.Empty;

    /// <summary>Whether the policyholder/LA is a staff / family member of the distributor.</summary>
    public bool IsStaffFamily { get; set; }

    /// <summary>Age at risk commencement (typically = Entry Age for adults).</summary>
    public int AgeRiskCommencement { get; set; }

    /// <summary>Whether standard age proof has been submitted for the Life Assured (Yes/No).</summary>
    public bool StandardAgeProofLA { get; set; } = true;

    /// <summary>Whether standard age proof has been submitted for the Policyholder (Yes/No).</summary>
    public bool StandardAgeProofPH { get; set; } = true;

    // ---- EMR / extra charges ----

    /// <summary>EMR class for Life Assured: Standard, or EMR level (1-9).</summary>
    public string EmrClassLifeAssured { get; set; } = "Standard";

    /// <summary>EMR class for Policyholder: Standard, or EMR level (1-9).</summary>
    public string EmrClassPolicyholder { get; set; } = "Standard";

    /// <summary>Flat Extra per ₹1,000 Sum At Risk for Life Assured (occupational/avocation loading).</summary>
    public decimal FlatExtraLifeAssured { get; set; }

    /// <summary>Flat Extra per ₹1,000 Sum At Risk for Policyholder.</summary>
    public decimal FlatExtraPolicyholder { get; set; }

    /// <summary>Whether Kerala Flood Cess is applicable (state-specific levy).</summary>
    public bool KeralaFloodCess { get; set; }
}

/// <summary>
/// Single policy-year row of the legacy ULIP benefit illustration (dual-scenario).
/// Kept for backward compatibility. New code should use PartARows / PartBRows.
/// </summary>
public class UlipIllustrationRow
{
    /// <summary>Policy Year.</summary>
    public int Year { get; set; }

    /// <summary>Age of life assured at start of this policy year.</summary>
    public int Age { get; set; }

    /// <summary>Annualized premium due this year (AP; 0 after PPT).</summary>
    public decimal AnnualPremium { get; set; }

    /// <summary>Premium invested after Premium Allocation Charge (= AP × (1 − PAC%)).</summary>
    public decimal PremiumInvested { get; set; }

    /// <summary>Total Mortality Charge (MC) deducted during this year (all monthly MCs summed).</summary>
    public decimal MortalityCharge { get; set; }

    /// <summary>Total Policy Administration Charge (PAC) deducted this year (₹100/month × active months).</summary>
    public decimal PolicyCharge { get; set; }

    // 4% assumed return scenario
    /// <summary>Fund Value at end of year — 4% gross return scenario.</summary>
    public decimal FundValue4 { get; set; }
    /// <summary>Death Benefit — 4% scenario: max(SA, FV, 105% × total premiums paid).</summary>
    public decimal DeathBenefit4 { get; set; }

    // 8% assumed return scenario
    /// <summary>Fund Value at end of year — 8% gross return scenario.</summary>
    public decimal FundValue8 { get; set; }
    /// <summary>Death Benefit — 8% scenario: max(SA, FV, 105% × total premiums paid).</summary>
    public decimal DeathBenefit8 { get; set; }
}

// ---------------------------------------------------------------------------
// Part A — Summary view (IRDAI BI format, both rate scenarios side-by-side)
// ---------------------------------------------------------------------------

/// <summary>Part A row: summary charges + fund value + SV + DB for one policy year.</summary>
public class PartARow
{
    /// <summary>Policy Year (1 to PT).</summary>
    public int Year { get; set; }

    /// <summary>Annualized Premium due in this year (0 after PPT).</summary>
    public decimal AnnualizedPremium { get; set; }

    // ---- 4% gross return scenario ----
    /// <summary>Total Mortality Charges for the year — 4% scenario.</summary>
    public decimal MortalityCharges4 { get; set; }
    /// <summary>Additional Risk Benefit Charges — 4% scenario (Platinum Plus only).</summary>
    public decimal ArbCharges4 { get; set; }
    /// <summary>Other Charges* = PAC + FMC — 4% scenario.</summary>
    public decimal OtherCharges4 { get; set; }
    /// <summary>GST on charges — 4% scenario (0% per current product rules).</summary>
    public decimal Gst4 { get; set; }
    /// <summary>Fund Value at end of year — 4% scenario.</summary>
    public decimal FundAtEndOfYear4 { get; set; }
    /// <summary>Surrender Value — 4% scenario.</summary>
    public decimal SurrenderValue4 { get; set; }
    /// <summary>Death Benefit — 4% scenario.</summary>
    public decimal DeathBenefit4 { get; set; }

    // ---- 8% gross return scenario ----
    /// <summary>Total Mortality Charges for the year — 8% scenario.</summary>
    public decimal MortalityCharges8 { get; set; }
    /// <summary>Additional Risk Benefit Charges — 8% scenario (Platinum Plus only).</summary>
    public decimal ArbCharges8 { get; set; }
    /// <summary>Other Charges* = PAC + FMC — 8% scenario.</summary>
    public decimal OtherCharges8 { get; set; }
    /// <summary>GST on charges — 8% scenario.</summary>
    public decimal Gst8 { get; set; }
    /// <summary>Fund Value at end of year — 8% scenario.</summary>
    public decimal FundAtEndOfYear8 { get; set; }
    /// <summary>Surrender Value — 8% scenario.</summary>
    public decimal SurrenderValue8 { get; set; }
    /// <summary>Death Benefit — 8% scenario.</summary>
    public decimal DeathBenefit8 { get; set; }
}

// ---------------------------------------------------------------------------
// Part B — Detailed charge break-up (one scenario per table)
// ---------------------------------------------------------------------------

/// <summary>Part B row: detailed charge and fund projection for one rate scenario per year.</summary>
public class PartBRow
{
    /// <summary>Policy Year.</summary>
    public int Year { get; set; }

    /// <summary>Annualized Premium (AP) due in this year (0 after PPT).</summary>
    public decimal AnnualizedPremium { get; set; }

    /// <summary>Premium Allocation Charge (PAC) deducted from AP before investment (usually 0).</summary>
    public decimal PremiumAllocationCharge { get; set; }

    /// <summary>AP after PAC = AP − PremiumAllocationCharge.</summary>
    public decimal AnnualizedPremiumAfterPac { get; set; }

    /// <summary>Total annual Mortality Charges (MC) — sum of monthly charges.</summary>
    public decimal MortalityCharges { get; set; }

    /// <summary>Additional Risk Benefit Charges (Platinum Plus only).</summary>
    public decimal ArbCharges { get; set; }

    /// <summary>GST on mortality / ARB charges (0% per current rules).</summary>
    public decimal Gst { get; set; }

    /// <summary>Total Policy Administration Charge for the year (₹100/month × months, first 10 years).</summary>
    public decimal PolicyAdministrationCharges { get; set; }

    /// <summary>Extra Premium Allocation (0 for standard plans).</summary>
    public decimal ExtraPremiumAllocation { get; set; }

    /// <summary>
    /// Fund value at end of anniversary month BEFORE that month's FMC.
    /// (Used to derive the Fund at End of Year figure.)
    /// </summary>
    public decimal FundBeforeFmc { get; set; }

    /// <summary>Total Fund Management Charge for the year (sum of all monthly FMC deductions).</summary>
    public decimal FundManagementCharge { get; set; }

    /// <summary>Loyalty Addition credited at this anniversary (0.10% × avg 12-month FV, years 6–PPT).</summary>
    public decimal LoyaltyAddition { get; set; }

    /// <summary>Wealth Booster credited at this anniversary (3% × avg 24-month FV, years 10, 15, 20…).</summary>
    public decimal WealthBooster { get; set; }

    /// <summary>
    /// Return of Charges credited:
    /// — At year 10: Return of all Policy Admin Charges paid.
    /// — At maturity: Return of all Mortality Charges paid.
    /// </summary>
    public decimal ReturnOfCharges { get; set; }

    /// <summary>Fund Value at end of year (after all monthly charges, then anniversary additions).</summary>
    public decimal FundAtEndOfYear { get; set; }

    /// <summary>Surrender Value = FundAtEndOfYear − Discontinuance Charge (years 1–4).</summary>
    public decimal SurrenderValue { get; set; }

    /// <summary>Death Benefit = max(SA, FundAtEndOfYear, 105% × TotalPremiumsPaid).</summary>
    public decimal DeathBenefit { get; set; }
}

// ---------------------------------------------------------------------------
// Response
// ---------------------------------------------------------------------------

/// <summary>Full ULIP Benefit Illustration response including Part A and Part B for both rate scenarios.</summary>
public class UlipCalculationResponse
{
    // -- Policy inputs echoed back --
    public string PolicyNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Option { get; set; } = "Platinum";
    public bool LifeAssuredSameAsPolicyholder { get; set; }
    public string Gender { get; set; } = string.Empty;
    public int EntryAge { get; set; }
    public int PolicyTerm { get; set; }
    public int Ppt { get; set; }

    /// <summary>Annualized Premium (AP).</summary>
    public decimal AnnualizedPremium { get; set; }

    /// <summary>Sum Assured (SA).</summary>
    public decimal SumAssured { get; set; }

    public string PremiumFrequency { get; set; } = string.Empty;

    // -- Derived / calculated fields --

    /// <summary>Maturity Age = EntryAge + PolicyTerm (must be formula-derived, not hardcoded).</summary>
    public int MaturityAge { get; set; }

    /// <summary>Premium Installment = AnnualizedPremium × ModalFactor (depends on PremiumFrequency).</summary>
    public decimal PremiumInstallment { get; set; }

    /// <summary>Net Yield at 4% gross return (IRR of premiums vs maturity fund value).</summary>
    public decimal NetYield4 { get; set; }

    /// <summary>Net Yield at 8% gross return (IRR of premiums vs maturity fund value).</summary>
    public decimal NetYield8 { get; set; }

    // -- Summary values --

    /// <summary>Maturity Benefit at 4% assumed gross return (FV at final year).</summary>
    public decimal MaturityBenefit4 { get; set; }

    /// <summary>Maturity Benefit at 8% assumed gross return (FV at final year).</summary>
    public decimal MaturityBenefit8 { get; set; }

    // -- IRDAI disclaimer --
    /// <summary>IRDAI-mandated investment risk disclaimer.</summary>
    public string IrdaiDisclaimer { get; set; } =
        "In this policy, the investment risk in the investment portfolio is borne by the policyholder. " +
        "The linked insurance products do not offer any liquidity during the first five years of the contract. " +
        "The policyholder will not be able to surrender/withdraw the monies invested in linked insurance products " +
        "completely or partially till the end of fifth year. " +
        "Returns shown at 4% p.a. and 8% p.a. are for illustration purposes only and are not guaranteed.";

    // -- Part A: dual-scenario summary table --
    /// <summary>Part A rows — one per policy year, showing both 4% and 8% scenarios.</summary>
    public List<PartARow> PartARows { get; set; } = new();

    // -- Part B: detailed charge tables for each scenario --
    /// <summary>Part B rows for 4% assumed gross return.</summary>
    public List<PartBRow> PartBRows4 { get; set; } = new();
    /// <summary>Part B rows for 8% assumed gross return.</summary>
    public List<PartBRow> PartBRows8 { get; set; } = new();

    /// <summary>
    /// Legacy yearly table for backward compatibility.
    /// Each row has MC/PolicyCharge as the average of 4% and 8% scenarios.
    /// </summary>
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
