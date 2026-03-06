using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Controllers;

/// <summary>Audit module endpoints — payout verification and bonus calculation.</summary>
[ApiController]
[Route("api/audit")]
[Produces("application/json")]
public class AuditController : ControllerBase
{
    private readonly InsuranceDbContext _db;
    private readonly IBenefitCalculationService _calcService;

    public AuditController(InsuranceDbContext db, IBenefitCalculationService calcService)
    {
        _db = db;
        _calcService = calcService;
    }

    // -------------------------------------------------------------------------
    // Payout Verification
    // -------------------------------------------------------------------------

    /// <summary>
    /// Single-policy payout verification.
    /// Calculates expected payout using existing Century Income formulas
    /// and compares it with the system payout provided by the user.
    /// </summary>
    [HttpPost("payout/single")]
    [ProducesResponseType(typeof(PayoutVerificationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifySinglePayout([FromBody] PayoutVerificationRequest req)
    {
        var biReq = new BenefitIllustrationRequest
        {
            AnnualPremium = req.AnnualPremium,
            Ppt = req.PremiumPayingTerm,
            PolicyTerm = req.PolicyTerm,
            EntryAge = req.EntryAge,
            Option = req.Option,
            Channel = req.Channel ?? "Other",
            PremiumsPaid = req.PremiumsPaid
        };

        var result = await _calcService.CalculateAsync(biReq);

        // Use maturity benefit as expected payout
        var expectedPayout = result.GuaranteedMaturityBenefit;
        var variance = expectedPayout - req.SystemPayout;
        var variancePct = expectedPayout != 0 ? (variance / expectedPayout) * 100m : 0m;
        var isMatch = Math.Abs(variancePct) <= 1m; // within 1% tolerance

        // Log
        try
        {
            _db.CalculationLogs.Add(new Models.CalculationLog
            {
                Module = "Audit-Payout",
                ProductType = req.ProductCode ?? "CENTURY_INCOME",
                PolicyNumber = req.PolicyNumber,
                InputJson = System.Text.Json.JsonSerializer.Serialize(req),
                ResultJson = $"{{\"expectedPayout\":{expectedPayout},\"systemPayout\":{req.SystemPayout},\"status\":\"{(isMatch ? "Match" : "Mismatch")}\"}}",
                RequestedBy = User.Identity?.Name ?? "Anonymous",
                Status = "Completed"
            });
            await _db.SaveChangesAsync();
        }
        catch { /* non-critical */ }

        return Ok(new PayoutVerificationResponse
        {
            PolicyNumber = req.PolicyNumber,
            ExpectedPayout = Round(expectedPayout),
            SystemPayout = Round(req.SystemPayout),
            Variance = Round(variance),
            VariancePct = Math.Round(variancePct, 2),
            Status = isMatch ? "Match" : "Mismatch"
        });
    }

    /// <summary>
    /// Batch payout verification (Excel upload).
    /// Returns per-row verification results.
    /// </summary>
    [HttpPost("payout/batch")]
    [ProducesResponseType(typeof(PayoutBatchResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BatchPayout([FromBody] PayoutBatchRequest req)
    {
        var results = new List<PayoutVerificationResponse>();
        foreach (var row in req.Rows)
        {
            var biReq = new BenefitIllustrationRequest
            {
                AnnualPremium = row.AnnualPremium,
                Ppt = row.PremiumPayingTerm,
                PolicyTerm = row.PolicyTerm,
                EntryAge = row.EntryAge,
                Option = row.Option ?? "Immediate",
                Channel = row.Channel ?? "Other",
                PremiumsPaid = row.PremiumsPaid
            };

            var result = await _calcService.CalculateAsync(biReq);
            var expectedPayout = result.GuaranteedMaturityBenefit;
            var variance = expectedPayout - row.SystemPayout;
            var variancePct = expectedPayout != 0 ? (variance / expectedPayout) * 100m : 0m;
            var isMatch = Math.Abs(variancePct) <= 1m;

            results.Add(new PayoutVerificationResponse
            {
                PolicyNumber = row.PolicyNumber,
                ExpectedPayout = Round(expectedPayout),
                SystemPayout = Round(row.SystemPayout),
                Variance = Round(variance),
                VariancePct = Math.Round(variancePct, 2),
                Status = isMatch ? "Match" : "Mismatch"
            });
        }

        return Ok(new PayoutBatchResponse
        {
            TotalRows = results.Count,
            MatchCount = results.Count(r => r.Status == "Match"),
            MismatchCount = results.Count(r => r.Status == "Mismatch"),
            Rows = results
        });
    }

    // -------------------------------------------------------------------------
    // Bonus / Addition Calculation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Single-policy bonus / addition calculation.
    /// Calculates loyalty income addition and total bonus for the given policy year.
    /// </summary>
    [HttpPost("bonus/single")]
    [ProducesResponseType(typeof(BonusCalculationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateSingleBonus([FromBody] BonusCalculationRequest req)
    {
        var biReq = new BenefitIllustrationRequest
        {
            AnnualPremium = req.AnnualPremium,
            Ppt = req.PremiumPayingTerm,
            PolicyTerm = req.PolicyTerm,
            EntryAge = req.EntryAge,
            Option = req.Option ?? "Immediate",
            Channel = req.Channel ?? "Other",
            PremiumsPaid = req.PolicyYear
        };

        var result = await _calcService.CalculateAsync(biReq);

        // Get the row for the requested policy year
        var row = result.YearlyTable.FirstOrDefault(r => r.PolicyYear == req.PolicyYear)
                  ?? result.YearlyTable.LastOrDefault();

        var bonusAddition = row?.LoyaltyIncome ?? 0m;
        var additionalBenefit = row?.TotalIncome ?? 0m;

        return Ok(new BonusCalculationResponse
        {
            PolicyNumber = req.PolicyNumber,
            PolicyYear = req.PolicyYear,
            BonusAddition = Round(bonusAddition),
            AdditionalBenefit = Round(additionalBenefit),
            TotalWithBonus = Round(additionalBenefit + req.ExistingFundValue),
            GuaranteedIncome = Round(row?.GuaranteedIncome ?? 0m),
            LoyaltyIncome = Round(row?.LoyaltyIncome ?? 0m)
        });
    }

    /// <summary>Batch bonus calculation.</summary>
    [HttpPost("bonus/batch")]
    [ProducesResponseType(typeof(BonusBatchResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BatchBonus([FromBody] BonusBatchRequest req)
    {
        var results = new List<BonusCalculationResponse>();
        foreach (var row in req.Rows)
        {
            var biReq = new BenefitIllustrationRequest
            {
                AnnualPremium = row.AnnualPremium,
                Ppt = row.PremiumPayingTerm,
                PolicyTerm = row.PolicyTerm,
                EntryAge = row.EntryAge,
                Option = row.Option ?? "Immediate",
                Channel = row.Channel ?? "Other",
                PremiumsPaid = row.PolicyYear
            };

            var result = await _calcService.CalculateAsync(biReq);
            var yearRow = result.YearlyTable.FirstOrDefault(r => r.PolicyYear == row.PolicyYear)
                          ?? result.YearlyTable.LastOrDefault();

            results.Add(new BonusCalculationResponse
            {
                PolicyNumber = row.PolicyNumber,
                PolicyYear = row.PolicyYear,
                BonusAddition = Round(yearRow?.LoyaltyIncome ?? 0m),
                AdditionalBenefit = Round(yearRow?.TotalIncome ?? 0m),
                TotalWithBonus = Round((yearRow?.TotalIncome ?? 0m) + row.ExistingFundValue),
                GuaranteedIncome = Round(yearRow?.GuaranteedIncome ?? 0m),
                LoyaltyIncome = Round(yearRow?.LoyaltyIncome ?? 0m)
            });
        }

        return Ok(new BonusBatchResponse { TotalRows = results.Count, Rows = results });
    }

    // -------------------------------------------------------------------------
    // Recent calculations for dashboard
    // -------------------------------------------------------------------------

    /// <summary>Get recent calculation logs for the dashboard.</summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(IEnumerable<Models.CalculationLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int top = 10)
    {
        var logs = await _db.CalculationLogs
            .OrderByDescending(l => l.CreatedDate)
            .Take(top)
            .ToListAsync();
        return Ok(logs);
    }

    private static decimal Round(decimal v) =>
        Math.Round(v, 2, MidpointRounding.AwayFromZero);
}

// DTO classes
public class PayoutVerificationRequest
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public decimal AnnualPremium { get; set; }
    public int PolicyTerm { get; set; }
    public int PremiumPayingTerm { get; set; }
    public int PremiumsPaid { get; set; }
    public int EntryAge { get; set; }
    public string Option { get; set; } = "Immediate";
    public string? Channel { get; set; }
    public decimal SystemPayout { get; set; }
}

public class PayoutVerificationResponse
{
    public string PolicyNumber { get; set; } = string.Empty;
    public decimal ExpectedPayout { get; set; }
    public decimal SystemPayout { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePct { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PayoutBatchRequest
{
    public List<PayoutVerificationRequest> Rows { get; set; } = new();
}

public class PayoutBatchResponse
{
    public int TotalRows { get; set; }
    public int MatchCount { get; set; }
    public int MismatchCount { get; set; }
    public List<PayoutVerificationResponse> Rows { get; set; } = new();
}

public class BonusCalculationRequest
{
    public string PolicyNumber { get; set; } = string.Empty;
    public decimal AnnualPremium { get; set; }
    public int PolicyTerm { get; set; }
    public int PremiumPayingTerm { get; set; }
    public int PolicyYear { get; set; }
    public int EntryAge { get; set; }
    public string? Option { get; set; }
    public string? Channel { get; set; }
    public decimal ExistingFundValue { get; set; }
}

public class BonusCalculationResponse
{
    public string PolicyNumber { get; set; } = string.Empty;
    public int PolicyYear { get; set; }
    public decimal BonusAddition { get; set; }
    public decimal AdditionalBenefit { get; set; }
    public decimal TotalWithBonus { get; set; }
    public decimal GuaranteedIncome { get; set; }
    public decimal LoyaltyIncome { get; set; }
}

public class BonusBatchRequest
{
    public List<BonusCalculationRequest> Rows { get; set; } = new();
}

public class BonusBatchResponse
{
    public int TotalRows { get; set; }
    public List<BonusCalculationResponse> Rows { get; set; } = new();
}
