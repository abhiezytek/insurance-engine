using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Controllers;

/// <summary>YPYG (You Pay You Get) endpoints — policy lookup and calculation.</summary>
[ApiController]
[Route("api/ypyg")]
[Produces("application/json")]
public class YpygController : ControllerBase
{
    private readonly InsuranceDbContext _db;
    private readonly IBenefitCalculationService _calcService;
    private readonly IUlipCalculationService _ulipService;

    public YpygController(InsuranceDbContext db, IBenefitCalculationService calcService, IUlipCalculationService ulipService)
    {
        _db = db;
        _calcService = calcService;
        _ulipService = ulipService;
    }

    /// <summary>
    /// Look up a policy by policy number. Returns key policy fields
    /// that can be used to pre-populate the Input Value form.
    /// This is a demo endpoint — returns computed/default values when
    /// no live core-policy system is connected.
    /// </summary>
    [HttpGet("policy/{policyNumber}")]
    [ProducesResponseType(typeof(PolicyLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetPolicy(string policyNumber)
    {
        // In production this would call a Core Policy system.
        // For now we return a computed demo record so the UI can be tested end-to-end.
        if (string.IsNullOrWhiteSpace(policyNumber))
            return BadRequest(new { error = "Policy number is required." });

        // Seed-quality demo data keyed by last digit of policy number
        var seed = policyNumber.Length > 0 ? (int)(policyNumber[^1] - '0') % 3 : 0;
        var options = new[] { "Immediate", "Deferred", "Twin" };
        var channels = new[] { "Online", "StaffDirect", "Other" };

        var isUlip = policyNumber.StartsWith("UL", StringComparison.OrdinalIgnoreCase);

        if (isUlip)
        {
            var ulipOptions = new[] { "Platinum", "Platinum Plus", "Platinum Plus" };
            return Ok(new PolicyLookupResponse
            {
                PolicyNumber = policyNumber,
                CustomerName = $"Demo ULIP Customer ({policyNumber})",
                ProductType = "e-Wealth Royale",
                ProductCode = "EWEALTH-ROYALE",
                ProductCategory = "ULIP",
                Uin = "142L079V03",
                AnnualPremium = 100000 + seed * 25000,
                PolicyTerm = 15 + seed * 5,
                PremiumPayingTerm = 7 + seed,
                PremiumsPaid = 3 + seed,
                SumAssured = 1000000 + seed * 250000m,
                FundValue = 350000 + seed * 80000m,
                PolicyStatus = "In-Force",
                Option = ulipOptions[seed],
                Channel = channels[seed],
                EntryAge = 30 + seed * 2,
                Gender = seed == 1 ? "Female" : "Male",
                DateOfBirth = new DateTime(1990 - seed * 2, 6, 15),
                PremiumFrequency = "Yearly",
                PremiumStatus = "Paid",
                DateOfCommencement = new DateTime(2021, 4, 1),
                RiskCommencementDate = new DateTime(2021, 4, 1),
                PendingPremiums = 0,
                SurvivalBenefitPaid = 0m,
                InvestmentStrategy = seed == 0 ? "Self-Managed" : "Life-Stage Aggressive"
            });
        }

        return Ok(new PolicyLookupResponse
        {
            PolicyNumber = policyNumber,
            CustomerName = $"Demo Customer ({policyNumber})",
            ProductType = "Century Income",
            ProductCode = "CENTURY_INCOME",
            ProductCategory = "Traditional",
            Uin = "142N066V02",
            AnnualPremium = 50000 + seed * 10000,
            PolicyTerm = 20,
            PremiumPayingTerm = 10,
            PremiumsPaid = 5 + seed,
            SumAssured = 500000 + seed * 100000m,
            FundValue = 0m,
            PolicyStatus = "In-Force",
            Option = options[seed],
            Channel = channels[seed],
            EntryAge = 35 + seed,
            Gender = seed == 1 ? "Female" : "Male",
            DateOfBirth = new DateTime(1988 - seed, 3, 10),
            PremiumFrequency = "Yearly",
            PremiumStatus = "Paid",
            DateOfCommencement = new DateTime(2019, 1, 1),
            RiskCommencementDate = new DateTime(2019, 1, 1),
            PendingPremiums = 0,
            SurvivalBenefitPaid = seed * 15000m,
            InvestmentStrategy = string.Empty
        });
    }

    /// <summary>
    /// Run a YPYG calculation using the existing Century Income benefit formulas.
    /// Returns maturity value, surrender value, and death benefit table.
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(YpygCalculationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Calculate([FromBody] YpygCalculationRequest req)
    {
        if (req.ProductCategory == "ULIP")
            return await CalculateUlip(req);

        return await CalculateTraditional(req);
    }

    private async Task<IActionResult> CalculateTraditional(YpygCalculationRequest req)
    {
        var biReq = new BenefitIllustrationRequest
        {
            AnnualPremium = req.AnnualPremium,
            Ppt = req.PremiumPayingTerm,
            PolicyTerm = req.PolicyTerm,
            EntryAge = req.EntryAge,
            Option = req.Option,
            Channel = req.Channel,
            PremiumsPaid = req.PremiumsPaid,
            IsPreIssuance = false,
            RiskCommencementDate = req.RiskCommencementDate
        };

        var result = await _calcService.CalculateAsync(biReq);

        // Pull last year values for summary
        var lastRow = result.YearlyTable.Count > 0 ? result.YearlyTable[^1] : null;

        // Determine current policy year from risk commencement date
        int elapsedYears;
        if (req.RiskCommencementDate.HasValue)
        {
            elapsedYears = (int)((DateTime.UtcNow - req.RiskCommencementDate.Value).TotalDays / 365.25);
            elapsedYears = Math.Max(1, Math.Min(elapsedYears, req.PolicyTerm));
        }
        else
        {
            elapsedYears = Math.Max(1, req.PremiumsPaid);
        }

        var currentRow = result.YearlyTable.FirstOrDefault(r => r.PolicyYear == elapsedYears)
                         ?? result.YearlyTable.FirstOrDefault();

        var response = new YpygCalculationResponse
        {
            PolicyNumber = req.PolicyNumber,
            ProductCode = req.ProductCode,
            ProductCategory = "Traditional",
            Uin = req.Uin,
            CustomerName = req.CustomerName,
            PolicyStatus = req.PolicyStatus,
            AnnualPremium = req.AnnualPremium,
            PolicyTerm = req.PolicyTerm,
            PremiumPayingTerm = req.PremiumPayingTerm,
            MaturityValue = result.GuaranteedMaturityBenefit,
            SurrenderValue = lastRow?.SurrenderValue ?? 0m,
            DeathBenefit = lastRow?.DeathBenefit ?? 0m,
            SumAssuredOnDeath = result.SumAssuredOnDeath,
            MaxLoanAmount = result.MaxLoanAmount,
            YearlyTable = result.YearlyTable.Select(r => new YpygYearlyRow
            {
                PolicyYear = r.PolicyYear,
                AnnualPremium = r.AnnualPremium,
                TotalPremiumsPaid = r.TotalPremiumsPaid,
                GuaranteedIncome = r.GuaranteedIncome,
                LoyaltyIncome = r.LoyaltyIncome,
                TotalIncome = r.TotalIncome,
                SurrenderValue = r.SurrenderValue,
                DeathBenefit = r.DeathBenefit,
                MaturityBenefit = r.MaturityBenefit
            }).ToList(),

            // Total Benefit Value fields
            CalculationDate = DateTime.UtcNow,
            CurrentPolicyYear = elapsedYears,
            CurrentSurvivalBenefit = currentRow?.CumulativeSurvivalBenefits ?? 0m,
            MaturitySurvivalBenefit = lastRow?.CumulativeSurvivalBenefits ?? 0m,
            CurrentMaturityBenefit = currentRow?.SurrenderValue ?? 0m,
            MaturityMaturityBenefit = result.GuaranteedMaturityBenefit,
            CurrentDeathBenefit = currentRow?.DeathBenefit ?? 0m,
            MaturityDeathBenefit = lastRow?.DeathBenefit ?? 0m,
        };

        await LogCalculation("YPYG-Endowment", req, response);
        return Ok(response);
    }

    private async Task<IActionResult> CalculateUlip(YpygCalculationRequest req)
    {
        var ulipReq = new UlipCalculationRequest
        {
            PolicyNumber = req.PolicyNumber,
            CustomerName = req.CustomerName,
            ProductCode = req.ProductCode,
            Gender = req.Gender,
            DateOfBirth = req.DateOfBirth ?? (req.RiskCommencementDate ?? DateTime.UtcNow).AddYears(-req.EntryAge),
            EntryAge = req.EntryAge,
            PolicyTerm = req.PolicyTerm,
            Ppt = req.PremiumPayingTerm,
            AnnualizedPremium = req.AnnualPremium,
            SumAssured = req.SumAssured,
            PremiumFrequency = req.PremiumFrequency,
            Option = req.Option,
            InvestmentStrategy = req.InvestmentStrategy,
            PolicyEffectiveDate = req.RiskCommencementDate
        };

        var result = await _ulipService.CalculateAsync(ulipReq);

        // Determine current policy year from risk commencement date
        int elapsedYears;
        if (req.RiskCommencementDate.HasValue)
        {
            elapsedYears = (int)((DateTime.UtcNow - req.RiskCommencementDate.Value).TotalDays / 365.25);
            elapsedYears = Math.Max(1, Math.Min(elapsedYears, req.PolicyTerm));
        }
        else
        {
            elapsedYears = Math.Max(1, req.PremiumsPaid);
        }

        // Build the ULIP yearly table from the legacy YearlyTable + PartARows for surrender values
        var ulipYearlyRows = new List<YpygUlipYearlyRow>();
        for (int i = 0; i < result.YearlyTable.Count; i++)
        {
            var row = result.YearlyTable[i];
            var partA = i < result.PartARows.Count ? result.PartARows[i] : null;
            ulipYearlyRows.Add(new YpygUlipYearlyRow
            {
                Year = row.Year,
                Age = row.Age,
                AnnualPremium = row.AnnualPremium,
                PremiumInvested = row.PremiumInvested,
                MortalityCharge = row.MortalityCharge,
                PolicyCharge = row.PolicyCharge,
                FundValue4 = row.FundValue4,
                DeathBenefit4 = row.DeathBenefit4,
                SurrenderValue4 = partA?.SurrenderValue4 ?? 0m,
                FundValue8 = row.FundValue8,
                DeathBenefit8 = row.DeathBenefit8,
                SurrenderValue8 = partA?.SurrenderValue8 ?? 0m
            });
        }

        // Get final-year PartA for summary fund values
        var lastPartA = result.PartARows.Count > 0 ? result.PartARows[^1] : null;

        // Get current-year row for current-date values
        var currentYearIndex = Math.Max(0, Math.Min(elapsedYears - 1, result.YearlyTable.Count - 1));
        var currentYearRow = result.YearlyTable.Count > 0 ? result.YearlyTable[currentYearIndex] : null;
        var currentPartA = currentYearIndex < result.PartARows.Count ? result.PartARows[currentYearIndex] : null;

        var response = new YpygCalculationResponse
        {
            PolicyNumber = req.PolicyNumber,
            ProductCode = req.ProductCode,
            ProductCategory = "ULIP",
            Uin = req.Uin,
            CustomerName = req.CustomerName,
            PolicyStatus = req.PolicyStatus,
            AnnualPremium = req.AnnualPremium,
            PolicyTerm = req.PolicyTerm,
            PremiumPayingTerm = req.PremiumPayingTerm,
            MaturityValue = result.MaturityBenefit8,
            SurrenderValue = lastPartA?.SurrenderValue8 ?? 0m,
            DeathBenefit = lastPartA?.DeathBenefit8 ?? 0m,
            SumAssuredOnDeath = result.SumAssured,
            FundValue4 = lastPartA?.FundAtEndOfYear4,
            FundValue8 = lastPartA?.FundAtEndOfYear8,
            MaturityBenefit4 = result.MaturityBenefit4,
            MaturityBenefit8 = result.MaturityBenefit8,
            UlipYearlyTable = ulipYearlyRows,

            // Total Benefit Value fields
            CalculationDate = DateTime.UtcNow,
            CurrentPolicyYear = elapsedYears,
            CurrentSurvivalBenefit = 0m,
            MaturitySurvivalBenefit = 0m,
            CurrentMaturityBenefit = currentPartA?.SurrenderValue8 ?? currentYearRow?.FundValue8 ?? 0m,
            MaturityMaturityBenefit = result.MaturityBenefit8,
            CurrentDeathBenefit = currentYearRow?.DeathBenefit8 ?? 0m,
            MaturityDeathBenefit = lastPartA?.DeathBenefit8 ?? 0m,
            CurrentFundValue4 = currentYearRow?.FundValue4 ?? 0m,
            CurrentFundValue8 = currentYearRow?.FundValue8 ?? 0m,
            MaturityFundValue4 = lastPartA?.FundAtEndOfYear4 ?? 0m,
            MaturityFundValue8 = lastPartA?.FundAtEndOfYear8 ?? 0m,
            CurrentDeathBenefit4 = currentYearRow?.DeathBenefit4 ?? 0m,
            CurrentDeathBenefit8 = currentYearRow?.DeathBenefit8 ?? 0m,
            MaturityDeathBenefit4 = lastPartA?.DeathBenefit4 ?? 0m,
            MaturityDeathBenefit8 = lastPartA?.DeathBenefit8 ?? 0m,
        };

        await LogCalculation("YPYG-ULIP", req, response);
        return Ok(response);
    }

    private async Task LogCalculation(string module, YpygCalculationRequest req, YpygCalculationResponse response)
    {
        try
        {
            _db.CalculationLogs.Add(new Models.CalculationLog
            {
                Module = module,
                ProductType = req.ProductCode,
                PolicyNumber = req.PolicyNumber,
                InputJson = $"{{\"annualPremium\":{req.AnnualPremium},\"policyTerm\":{req.PolicyTerm},\"ppt\":{req.PremiumPayingTerm}}}",
                ResultJson = $"{{\"maturityValue\":{response.MaturityValue},\"surrenderValue\":{response.SurrenderValue}}}",
                RequestedBy = User.Identity?.Name ?? "Anonymous",
                Status = "Completed"
            });
            await _db.SaveChangesAsync();
        }
        catch { /* non-critical */ }
    }
}

public class PolicyLookupResponse
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = "Traditional";
    public string Uin { get; set; } = string.Empty;
    public decimal AnnualPremium { get; set; }
    public int PolicyTerm { get; set; }
    public int PremiumPayingTerm { get; set; }
    public int PremiumsPaid { get; set; }
    public decimal SumAssured { get; set; }
    public decimal FundValue { get; set; }
    public string PolicyStatus { get; set; } = string.Empty;
    public string Option { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public int EntryAge { get; set; }
    public string Gender { get; set; } = "Male";
    public DateTime DateOfBirth { get; set; }
    public string PremiumFrequency { get; set; } = "Yearly";
    public string PremiumStatus { get; set; } = "Paid";
    public DateTime DateOfCommencement { get; set; }
    public DateTime RiskCommencementDate { get; set; }
    public int PendingPremiums { get; set; }
    public decimal SurvivalBenefitPaid { get; set; }
    public string InvestmentStrategy { get; set; } = string.Empty;
}

public class YpygCalculationRequest
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string ProductCode { get; set; } = "CENTURY_INCOME";
    public string ProductCategory { get; set; } = "Traditional";
    public string Uin { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Gender { get; set; } = "Male";
    public DateTime? DateOfBirth { get; set; }
    public string PremiumFrequency { get; set; } = "Yearly";
    public string PolicyStatus { get; set; } = "In-Force";
    public string InvestmentStrategy { get; set; } = "Self-Managed";
    public decimal AnnualPremium { get; set; }
    public int PolicyTerm { get; set; }
    public int PremiumPayingTerm { get; set; }
    public int PremiumsPaid { get; set; }
    public decimal SumAssured { get; set; }
    public int EntryAge { get; set; }
    public string Option { get; set; } = "Immediate";
    public string Channel { get; set; } = "Other";
    public decimal FundValue { get; set; }
    public decimal BonusRate { get; set; }
    public decimal SurrenderFactor { get; set; } = 0.8m;
    /// <summary>Risk Commencement Date — used to determine elapsed policy years in YPYG mode.</summary>
    public DateTime? RiskCommencementDate { get; set; }
}

public class YpygCalculationResponse
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = "Traditional";
    public string Uin { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PolicyStatus { get; set; } = "In-Force";
    public decimal AnnualPremium { get; set; }
    public int PolicyTerm { get; set; }
    public int PremiumPayingTerm { get; set; }
    public decimal MaturityValue { get; set; }
    public decimal SurrenderValue { get; set; }
    public decimal DeathBenefit { get; set; }
    public decimal SumAssuredOnDeath { get; set; }
    public decimal MaxLoanAmount { get; set; }
    public List<YpygYearlyRow> YearlyTable { get; set; } = new();
    // ULIP-specific fields (null/empty for Traditional).
    public decimal? FundValue4 { get; set; }
    public decimal? FundValue8 { get; set; }
    public decimal? MaturityBenefit4 { get; set; }
    public decimal? MaturityBenefit8 { get; set; }
    public List<YpygUlipYearlyRow>? UlipYearlyTable { get; set; }

    // ── Total Benefit Value fields (current-date vs maturity) ──
    public DateTime CalculationDate { get; set; }
    public int CurrentPolicyYear { get; set; }

    // Traditional current-date values
    public decimal CurrentSurvivalBenefit { get; set; }
    public decimal MaturitySurvivalBenefit { get; set; }
    public decimal CurrentMaturityBenefit { get; set; }
    public decimal MaturityMaturityBenefit { get; set; }
    public decimal CurrentDeathBenefit { get; set; }
    public decimal MaturityDeathBenefit { get; set; }

    // ULIP current-date values
    public decimal? CurrentFundValue4 { get; set; }
    public decimal? CurrentFundValue8 { get; set; }
    public decimal? MaturityFundValue4 { get; set; }
    public decimal? MaturityFundValue8 { get; set; }
    public decimal? CurrentDeathBenefit4 { get; set; }
    public decimal? CurrentDeathBenefit8 { get; set; }
    public decimal? MaturityDeathBenefit4 { get; set; }
    public decimal? MaturityDeathBenefit8 { get; set; }
}

public class YpygYearlyRow
{
    public int PolicyYear { get; set; }
    public decimal AnnualPremium { get; set; }
    public decimal TotalPremiumsPaid { get; set; }
    public decimal GuaranteedIncome { get; set; }
    public decimal LoyaltyIncome { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal SurrenderValue { get; set; }
    public decimal DeathBenefit { get; set; }
    public decimal MaturityBenefit { get; set; }
}

public class YpygUlipYearlyRow
{
    public int Year { get; set; }
    public int Age { get; set; }
    public decimal AnnualPremium { get; set; }
    public decimal PremiumInvested { get; set; }
    public decimal MortalityCharge { get; set; }
    public decimal PolicyCharge { get; set; }
    public decimal FundValue4 { get; set; }
    public decimal DeathBenefit4 { get; set; }
    public decimal SurrenderValue4 { get; set; }
    public decimal FundValue8 { get; set; }
    public decimal DeathBenefit8 { get; set; }
    public decimal SurrenderValue8 { get; set; }
}
