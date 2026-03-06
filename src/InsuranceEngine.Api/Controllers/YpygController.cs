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

    public YpygController(InsuranceDbContext db, IBenefitCalculationService calcService)
    {
        _db = db;
        _calcService = calcService;
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

        return Ok(new PolicyLookupResponse
        {
            PolicyNumber = policyNumber,
            CustomerName = $"Demo Customer ({policyNumber})",
            ProductType = "Century Income",
            ProductCode = "CENTURY_INCOME",
            AnnualPremium = 50000 + seed * 10000,
            PolicyTerm = 20,
            PremiumPayingTerm = 10,
            PremiumsPaid = 5 + seed,
            SumAssured = 500000 + seed * 100000m,
            FundValue = 0m,
            PolicyStatus = "In-Force",
            Option = options[seed],
            Channel = channels[seed],
            EntryAge = 35 + seed
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
        var biReq = new BenefitIllustrationRequest
        {
            AnnualPremium = req.AnnualPremium,
            Ppt = req.PremiumPayingTerm,
            PolicyTerm = req.PolicyTerm,
            EntryAge = req.EntryAge,
            Option = req.Option,
            Channel = req.Channel,
            PremiumsPaid = req.PremiumsPaid
        };

        var result = await _calcService.CalculateAsync(biReq);

        // Pull last year values for summary
        var lastRow = result.YearlyTable.Count > 0 ? result.YearlyTable[^1] : null;

        var response = new YpygCalculationResponse
        {
            PolicyNumber = req.PolicyNumber,
            ProductCode = req.ProductCode,
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
            }).ToList()
        };

        // Log the calculation
        try
        {
            _db.CalculationLogs.Add(new Models.CalculationLog
            {
                Module = "YPYG",
                ProductType = req.ProductCode,
                PolicyNumber = req.PolicyNumber,
                InputJson = System.Text.Json.JsonSerializer.Serialize(req),
                ResultJson = $"{{\"maturityValue\":{response.MaturityValue},\"surrenderValue\":{response.SurrenderValue}}}",
                RequestedBy = User.Identity?.Name ?? "Anonymous",
                Status = "Completed"
            });
            await _db.SaveChangesAsync();
        }
        catch { /* non-critical */ }

        return Ok(response);
    }
}

public class PolicyLookupResponse
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
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
}

public class YpygCalculationRequest
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string ProductCode { get; set; } = "CENTURY_INCOME";
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
}

public class YpygCalculationResponse
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal AnnualPremium { get; set; }
    public int PolicyTerm { get; set; }
    public int PremiumPayingTerm { get; set; }
    public decimal MaturityValue { get; set; }
    public decimal SurrenderValue { get; set; }
    public decimal DeathBenefit { get; set; }
    public decimal SumAssuredOnDeath { get; set; }
    public decimal MaxLoanAmount { get; set; }
    public List<YpygYearlyRow> YearlyTable { get; set; } = new();
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
