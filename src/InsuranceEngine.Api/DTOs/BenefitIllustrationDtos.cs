namespace InsuranceEngine.Api.DTOs;

/// <summary>Request for generating a yearly Benefit Illustration table.</summary>
public class BenefitIllustrationRequest
{
    /// <summary>Annual Premium (excluding taxes, riders, extra premium).</summary>
    public decimal AnnualPremium { get; set; }

    /// <summary>Premium Payment Term (number of years premiums are paid).</summary>
    public int Ppt { get; set; }

    /// <summary>Policy Term (total duration of the policy in years).</summary>
    public int PolicyTerm { get; set; }

    /// <summary>Age of the life assured at entry.</summary>
    public int EntryAge { get; set; }

    /// <summary>Income option: Immediate, Deferred, or Twin.</summary>
    public string Option { get; set; } = "Immediate";

    /// <summary>Sales channel: Online, StaffDirect, or Other.</summary>
    public string Channel { get; set; } = "Other";

    /// <summary>
    /// Number of premiums actually paid (for Reduced Paid-Up calculations).
    /// When null, assumed equal to PPT (fully paid-up policy).
    /// </summary>
    public int? PremiumsPaid { get; set; }
}

/// <summary>Full yearly Benefit Illustration table for a Century Income policy.</summary>
public class BenefitIllustrationResponse
{
    public decimal AnnualPremium { get; set; }
    public int Ppt { get; set; }
    public int PolicyTerm { get; set; }
    public int EntryAge { get; set; }
    public string Option { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;

    /// <summary>Sum Assured on Death (10 × AP).</summary>
    public decimal SumAssuredOnDeath { get; set; }

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

    /// <summary>Death benefit payable if death occurs in this policy year.</summary>
    public decimal DeathBenefit { get; set; }

    /// <summary>Maturity benefit (only non-zero in final policy year).</summary>
    public decimal MaturityBenefit { get; set; }

    /// <summary>Whether the policy is in reduced paid-up status in this year.</summary>
    public bool IsPaidUp { get; set; }
}
