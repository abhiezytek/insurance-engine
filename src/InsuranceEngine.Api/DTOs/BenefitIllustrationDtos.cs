namespace InsuranceEngine.Api.DTOs;

/// <summary>Request for generating a yearly Benefit Illustration table.</summary>
public class BenefitIllustrationRequest
{
    /// <summary>Product code (for formula selection).</summary>
    public string ProductCode { get; set; } = "CENTURY_INCOME";

    /// <summary>Optional product version (for formula selection).</summary>
    public string? ProductVersion { get; set; }

    /// <summary>Optional factor table version tag.</summary>
    public string? FactorVersion { get; set; }

    /// <summary>Optional formula version tag.</summary>
    public string? FormulaVersion { get; set; }

    /// <summary>
    /// Annualised Premium — base premium payable in a year excluding taxes, rider premiums,
    /// underwriting extra premiums, and modal/frequency loading.
    /// When provided, InstallmentPremium and AnnualPremium are derived using modal factors.
    /// </summary>
    public decimal? AnnualisedPremium { get; set; }

    /// <summary>
    /// Annual Premium — backward-compatible fallback.
    /// Used only when AnnualisedPremium is not provided.
    /// </summary>
    public decimal AnnualPremium { get; set; }

    /// <summary>Premium Payment Term (number of years premiums are paid).</summary>
    public int Ppt { get; set; }

    /// <summary>Policy Term (total duration of the policy in years).</summary>
    public int PolicyTerm { get; set; }

    /// <summary>Age of the life assured at entry.</summary>
    public int EntryAge { get; set; }

    /// <summary>Date of birth of the Life Assured.</summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>Name of the Life Assured.</summary>
    public string? NameOfLifeAssured { get; set; }

    /// <summary>Name of the Policy Holder.</summary>
    public string? NameOfPolicyHolder { get; set; }

    /// <summary>Age of the Policy Holder.</summary>
    public int? AgeOfPolicyHolder { get; set; }

    /// <summary>Date of birth of the Policy Holder.</summary>
    public DateTime? PolicyholderDateOfBirth { get; set; }

    /// <summary>Whether Life Assured and Proposer are the same person.</summary>
    public bool LifeAssuredSameAsProposer { get; set; } = true;

    /// <summary>Income option: Immediate, Deferred, or Twin.</summary>
    public string Option { get; set; } = "Immediate";

    /// <summary>Sales channel (e.g. Corporate Agency, Direct Marketing, Online, Broker, Agency, Web Aggregator, Insurance Marketing Firm).</summary>
    public string Channel { get; set; } = "Agency";

    /// <summary>Gender of the life assured: Male or Female.</summary>
    public string Gender { get; set; } = "Male";

    /// <summary>Premium payment frequency/mode: Yearly, Half Yearly, Quarterly, Monthly.</summary>
    public string PremiumFrequency { get; set; } = "Yearly";

    /// <summary>Whether standard age proof has been submitted (Yes/No).</summary>
    public bool StandardAgeProof { get; set; } = true;

    /// <summary>Whether this is a staff policy.</summary>
    public bool StaffPolicy { get; set; }

    /// <summary>
    /// Number of premiums actually paid (for Reduced Paid-Up calculations).
    /// When null, assumed equal to PPT (fully paid-up policy).
    /// </summary>
    public int? PremiumsPaid { get; set; }
    /// <summary>
    /// Optional explicit Sum Assured override.
    /// When null, SA on Death is derived as 10 × Annual Premium (per product wording).
    /// </summary>
    public decimal? SumAssured { get; set; }

    /// <summary>
    /// When true the illustration is pre-issuance (no policy/issuance date logic applied).
    /// When false (YPYG mode) the Risk Commencement Date is used.
    /// </summary>
    public bool IsPreIssuance { get; set; } = true;

    /// <summary>
    /// Risk Commencement Date — used in YPYG mode (IsPreIssuance = false).
    /// Null means today's date (pre-issuance / BI mode).
    /// </summary>
    public DateTime? RiskCommencementDate { get; set; }
}

/// <summary>Full yearly Benefit Illustration table for an Endowment policy.</summary>
public class BenefitIllustrationResponse
{
    /// <summary>Product code (echo).</summary>
    public string ProductCode { get; set; } = "CENTURY_INCOME";

    /// <summary>Product version / formula version tag (optional).</summary>
    public string? ProductVersion { get; set; }

    /// <summary>Factor table version tag (optional).</summary>
    public string? FactorVersion { get; set; }

    /// <summary>Formula version tag (optional).</summary>
    public string? FormulaVersion { get; set; }

    /// <summary>
    /// Annualised Premium — the base premium amount payable in a year excluding taxes,
    /// rider premiums, underwriting extra premiums, and modal/frequency loading.
    /// Used as basis for GI, GMB, and survival benefit calculations.
    /// </summary>
    public decimal AnnualisedPremium { get; set; }

    /// <summary>
    /// Annual Premium — premium amount actually payable in a year (Installment Premium × Payments/Year).
    /// Includes modal/frequency loading. Used as basis for Sum Assured on Death.
    /// </summary>
    public decimal AnnualPremium { get; set; }

    /// <summary>Installment premium per payment event (shows modal factor impact).</summary>
    public decimal InstallmentPremium { get; set; }

    /// <summary>Modal factor applied for the chosen premium frequency.</summary>
    public decimal ModalFactor { get; set; }

    public int Ppt { get; set; }
    public int PolicyTerm { get; set; }
    public int EntryAge { get; set; }
    public string Option { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;

    /// <summary>Premium payment frequency used for this illustration.</summary>
    public string PremiumFrequency { get; set; } = string.Empty;

    /// <summary>Sum Assured on Death (10 × Annual Premium, per product wording).</summary>
    public decimal SumAssuredOnDeath { get; set; }

    /// <summary>Sum Assured on Maturity (GMB derived from Annualised Premium × GMB factor). Not displayed on frontend.</summary>
    public decimal SumAssuredOnMaturity { get; set; }

    /// <summary>Guaranteed Maturity Benefit (after High Premium + Channel benefits).</summary>
    public decimal GuaranteedMaturityBenefit { get; set; }

    /// <summary>Maximum loan amount (70% × final SV).</summary>
    public decimal MaxLoanAmount { get; set; }

    /// <summary>Yearly rows: one entry per policy year.</summary>
    public List<BenefitIllustrationRow> YearlyTable { get; set; } = new();
}

/// <summary>Single policy-year row of the benefit illustration.</summary>
public class BenefitIllustrationRow
{
    public int PolicyYear { get; set; }

    /// <summary>Annual premium due in this year (0 after PPT).</summary>
    public decimal AnnualPremium { get; set; }

    /// <summary>Total premiums paid cumulatively up to this year.</summary>
    public decimal TotalPremiumsPaid { get; set; }

    /// <summary>Guaranteed Income payable in this year.</summary>
    public decimal GuaranteedIncome { get; set; }

    /// <summary>Loyalty Income payable in this year.</summary>
    public decimal LoyaltyIncome { get; set; }

    /// <summary>Total income this year (GI + LI).</summary>
    public decimal TotalIncome { get; set; }

    /// <summary>Cumulative survival benefits paid up to this year.</summary>
    public decimal CumulativeSurvivalBenefits { get; set; }

    /// <summary>Guaranteed Surrender Value.</summary>
    public decimal Gsv { get; set; }

    /// <summary>Special Surrender Value.</summary>
    public decimal Ssv { get; set; }

    /// <summary>Surrender Value = MAX(GSV, SSV).</summary>
    public decimal SurrenderValue { get; set; }

    /// <summary>GSV factor applied for this policy year.</summary>
    public decimal GsvFactor { get; set; }

    /// <summary>SSV factor 1 applied for this policy year.</summary>
    public decimal SsvFactor1 { get; set; }

    /// <summary>SSV factor 2 applied for this policy year.</summary>
    public decimal SsvFactor2 { get; set; }

    /// <summary>Paid-up maturity benefit used in SSV computation.</summary>
    public decimal PaidUpMaturityBenefit { get; set; }

    /// <summary>Paid-up income component (GI/LI) used in SSV computation.</summary>
    public decimal PaidUpIncomeComponent { get; set; }

    /// <summary>Indicates whether GSV or SSV was the max for surrender value.</summary>
    public string SurrenderValueSource { get; set; } = "GSV";

    /// <summary>Death benefit payable if death occurs in this policy year.</summary>
    public decimal DeathBenefit { get; set; }

    /// <summary>Maturity benefit (only non-zero in final policy year).</summary>
    public decimal MaturityBenefit { get; set; }

    /// <summary>Whether the policy is in reduced paid-up status in this year.</summary>
    public bool IsPaidUp { get; set; }
}
