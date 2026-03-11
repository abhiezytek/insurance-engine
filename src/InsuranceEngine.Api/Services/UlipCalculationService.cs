using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Services;

/// <summary>
/// ULIP Benefit Illustration calculation service.
///
/// Abbreviations used throughout:
///   AP  = Annualized Premium
///   SA  = Sum Assured
///   PT  = Policy Term
///   PPT = Premium Payment Term
///   FV  = Fund Value (NAV)
///   FMC = Fund Management Charge  (1.35% p.a. = 0.00111809 monthly equivalent)
///   MC  = Mortality Charge         (SumAtRisk × MortalityRate / 1000)
///   PC  = Policy Administration Charge (₹100/month for first 10 yrs)
///   PAC = Premium Allocation Charge (0% — full premium invested)
///   SAR = Sum At Risk (SA - FV)
///   DB  = Death Benefit = Max(SA - PartialWithdrawals, FV, 105% × TotalPremiumsPaid)
///   LA  = Loyalty Addition (0.1% of FV after year 6)
///   WB  = Wealth Booster  (3% of FV at year 10, 15, 20, ...)
/// </summary>
public class UlipCalculationService : IUlipCalculationService
{
    // Default charge parameters — aligned to product specification
    private const decimal DefaultPremiumAllocationChargePct = 0m;     // 0% — full premium invested
    private const decimal DefaultPolicyAdminChargeMonthly   = 100m;   // ₹100/month for first 10 years
    private const decimal DefaultFmcPct                     = 1.35m;  // 1.35% p.a.

    // Loyalty Addition rate (% of FV per year after year 6)
    private const decimal LoyaltyAdditionPct = 0.1m;
    // Wealth Booster rate (% of FV at milestone years 10, 15, 20, ...)
    private const decimal WealthBoosterPct = 3.0m;

    private const decimal AssumedReturn4 = 0.04m;
    private const decimal AssumedReturn8 = 0.08m;

    private readonly InsuranceDbContext _db;

    public UlipCalculationService(InsuranceDbContext db) => _db = db;

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    public async Task<UlipCalculationResponse> CalculateAsync(UlipCalculationRequest req)
    {
        // 1. Load product
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Code == req.ProductCode && p.ProductType == "ULIP");
        var productName = product?.Name ?? req.ProductCode;

        // 2. Load charges for this product (or defaults)
        var charges = product != null
            ? await _db.UlipCharges.Where(c => c.ProductId == product.Id).ToListAsync()
            : new List<UlipCharge>();

        var pacPct   = GetCharge(charges, "PremiumAllocation", DefaultPremiumAllocationChargePct);
        var fmcPct   = GetCharge(charges, "FMC",              DefaultFmcPct);
        var pcMonthly = GetCharge(charges, "PolicyAdmin",     DefaultPolicyAdminChargeMonthly);

        // 3. Load mortality rates for gender
        var mortalityRates = await _db.MortalityRates
            .Where(m => m.Gender == req.Gender)
            .ToDictionaryAsync(m => m.Age, m => m.Rate);

        // Cumulative premiums paid (for death benefit calculation)
        decimal cumulativePremiumsPaid = 0m;

        // 4. Generate yearly rows
        var rows = new List<UlipIllustrationRow>();
        decimal fv4 = 0m, fv8 = 0m;

        for (int py = 1; py <= req.PolicyTerm; py++)
        {
            int age = req.EntryAge + py - 1;   // age at start of policy year

            // AP is paid only during PPT years
            var ap = py <= req.Ppt ? req.AnnualizedPremium : 0m;
            cumulativePremiumsPaid += ap;

            // Premium invested = AP × (1 − PAC%)  — PAC is 0% so full premium is invested
            var premiumInvested = Round(ap * (1m - pacPct / 100m));

            // Opening FV (carried over from prior year)
            var openFv4 = fv4;
            var openFv8 = fv8;

            // --- Policy Admin Charge: ₹100/month only for first 10 years ---
            var pcAnnual = py <= 10 ? Round(pcMonthly * 12m) : 0m;
            var pc = pcAnnual;

            // --- Mortality Charge ---
            // SAR = Max(SA − FV, 0);  MC = (SAR × MortalityRate) / 1000
            var mortalityRate = GetMortalityRate(mortalityRates, age);
            var sumAtRisk4 = Math.Max(req.SumAssured - openFv4, 0m);
            var sumAtRisk8 = Math.Max(req.SumAssured - openFv8, 0m);
            var mc4 = Round((sumAtRisk4 * mortalityRate) / 1000m);
            var mc8 = Round((sumAtRisk8 * mortalityRate) / 1000m);
            var mc  = Round((mc4 + mc8) / 2m);

            // --- Net amount for growth ---
            var net4 = Math.Max(0m, openFv4 + premiumInvested - mc4 - pc);
            var net8 = Math.Max(0m, openFv8 + premiumInvested - mc8 - pc);

            // Fund growth at assumed rates
            fv4 = Round(net4 * (1m + AssumedReturn4));
            fv8 = Round(net8 * (1m + AssumedReturn8));

            // Apply FMC after growth (1.35% p.a.)
            fv4 = Round(fv4 * (1m - fmcPct / 100m));
            fv8 = Round(fv8 * (1m - fmcPct / 100m));

            // --- Loyalty Addition: +0.1% of FV from year 7 onwards ---
            if (py >= 7)
            {
                fv4 = Round(fv4 * (1m + LoyaltyAdditionPct / 100m));
                fv8 = Round(fv8 * (1m + LoyaltyAdditionPct / 100m));
            }

            // --- Wealth Booster: +3% of FV at year 10, 15, 20, ... ---
            if (py >= 10 && (py - 10) % 5 == 0)
            {
                fv4 = Round(fv4 * (1m + WealthBoosterPct / 100m));
                fv8 = Round(fv8 * (1m + WealthBoosterPct / 100m));
            }

            fv4 = Math.Max(0m, fv4);
            fv8 = Math.Max(0m, fv8);

            // Death Benefit = Max(SA, FV, 105% × Cumulative Premiums Paid)
            var minDeath = Round(1.05m * cumulativePremiumsPaid);
            var db4 = Round(Math.Max(req.SumAssured, Math.Max(fv4, minDeath)));
            var db8 = Round(Math.Max(req.SumAssured, Math.Max(fv8, minDeath)));

            rows.Add(new UlipIllustrationRow
            {
                Year             = py,
                Age              = age,
                AnnualPremium    = Round(ap),
                PremiumInvested  = Round(premiumInvested),
                MortalityCharge  = mc,
                PolicyCharge     = pc,
                FundValue4       = fv4,
                DeathBenefit4    = db4,
                FundValue8       = fv8,
                DeathBenefit8    = db8,
            });
        }

        // 5. Maturity benefits = FV at end of PT
        var lastRow = rows.Count > 0 ? rows[^1] : null;

        // 6. Persist results (upsert — remove old rows first)
        await PersistResultsAsync(req, rows);

        return new UlipCalculationResponse
        {
            PolicyNumber       = req.PolicyNumber,
            CustomerName       = req.CustomerName,
            ProductCode        = req.ProductCode,
            ProductName        = productName,
            Gender             = req.Gender,
            EntryAge           = req.EntryAge,
            PolicyTerm         = req.PolicyTerm,
            Ppt                = req.Ppt,
            AnnualizedPremium  = Round(req.AnnualizedPremium),
            SumAssured         = Round(req.SumAssured),
            PremiumFrequency   = req.PremiumFrequency,
            MaturityBenefit4   = lastRow?.FundValue4 ?? 0m,
            MaturityBenefit8   = lastRow?.FundValue8 ?? 0m,
            YearlyTable        = rows,
        };
    }

    public async Task<UlipCalculationResponse?> GetByPolicyNumberAsync(string policyNumber)
    {
        var results = await _db.UlipIllustrationResults
            .Where(r => r.PolicyNumber == policyNumber)
            .OrderBy(r => r.Year)
            .ToListAsync();

        if (results.Count == 0) return null;

        var rows = results.Select(r => new UlipIllustrationRow
        {
            Year            = r.Year,
            Age             = r.Age,
            AnnualPremium   = r.Premium,
            PremiumInvested = r.PremiumInvested,
            MortalityCharge = r.MortalityCharge,
            PolicyCharge    = r.PolicyCharge,
            FundValue4      = r.FundValue4,
            DeathBenefit4   = r.DeathBenefit4,
            FundValue8      = r.FundValue8,
            DeathBenefit8   = r.DeathBenefit8,
        }).ToList();

        var last = rows[^1];

        return new UlipCalculationResponse
        {
            PolicyNumber      = policyNumber,
            CustomerName      = string.Empty,
            ProductCode       = string.Empty,
            YearlyTable       = rows,
            MaturityBenefit4  = last.FundValue4,
            MaturityBenefit8  = last.FundValue8,
        };
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private async Task PersistResultsAsync(UlipCalculationRequest req, List<UlipIllustrationRow> rows)
    {
        // Remove any existing rows for this policy number
        var existing = _db.UlipIllustrationResults.Where(r => r.PolicyNumber == req.PolicyNumber);
        _db.UlipIllustrationResults.RemoveRange(existing);

        foreach (var row in rows)
        {
            _db.UlipIllustrationResults.Add(new UlipIllustrationResult
            {
                PolicyNumber    = req.PolicyNumber,
                Year            = row.Year,
                Age             = row.Age,
                Premium         = row.AnnualPremium,
                PremiumInvested = row.PremiumInvested,
                MortalityCharge = row.MortalityCharge,
                PolicyCharge    = row.PolicyCharge,
                FundValue4      = row.FundValue4,
                FundValue8      = row.FundValue8,
                DeathBenefit4   = row.DeathBenefit4,
                DeathBenefit8   = row.DeathBenefit8,
            });
        }

        await _db.SaveChangesAsync();
    }

    private static decimal GetCharge(List<UlipCharge> charges, string chargeType, decimal defaultValue)
    {
        var match = charges.FirstOrDefault(c =>
            string.Equals(c.ChargeType, chargeType, StringComparison.OrdinalIgnoreCase));
        return match?.ChargeValue ?? defaultValue;
    }

    private static decimal GetMortalityRate(Dictionary<int, decimal> rates, int age)
    {
        if (rates.TryGetValue(age, out var rate)) return rate;

        // Nearest available age
        if (rates.Count == 0) return FallbackMortalityRate(age);

        var nearest = rates.MinBy(kv => Math.Abs(kv.Key - age));
        return nearest.Value;
    }

    /// <summary>
    /// Fallback Indian LIC-based approximate mortality rates (per 1000 sum at risk).
    /// Used when no mortality table is loaded in the database.
    /// </summary>
    private static decimal FallbackMortalityRate(int age) => age switch
    {
        <= 20 => 0.78m,
        <= 25 => 0.90m,
        <= 30 => 1.10m,
        <= 35 => 1.35m,
        <= 40 => 1.79m,
        <= 45 => 2.50m,
        <= 50 => 3.55m,
        <= 55 => 5.20m,
        <= 60 => 7.65m,
        <= 65 => 11.25m,
        _ => 16.00m,
    };

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
