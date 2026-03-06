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
///   FV  = Fund Value
///   FMC = Fund Management Charge  (% of FV, annual)
///   MC  = Mortality Charge         (SumAtRisk × MortalityRate / 1000)
///   PC  = Policy Administration Charge (monthly, charged annually)
///   PAC = Premium Allocation Charge (% of AP deducted before investment)
///   DB  = Death Benefit             = max(SA, FV)
/// </summary>
public class UlipCalculationService : IUlipCalculationService
{
    // Default charge parameters used when no rows exist in the database
    private const decimal DefaultPremiumAllocationChargePct = 5.0m;   // 5 %
    private const decimal DefaultPolicyAdminChargeMonthly   = 100m;    // ₹ 100 / month → ₹ 1200 / year
    private const decimal DefaultFmcPct                     = 1.35m;   // 1.35 % of FV per annum

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

        var pacPct  = GetCharge(charges, "PremiumAllocation",  DefaultPremiumAllocationChargePct);
        var fmcPct  = GetCharge(charges, "FMC",               DefaultFmcPct);
        var pcAnnual = GetCharge(charges, "PolicyAdmin",       DefaultPolicyAdminChargeMonthly * 12);

        // 3. Load mortality rates for gender
        var mortalityRates = await _db.MortalityRates
            .Where(m => m.Gender == req.Gender)
            .ToDictionaryAsync(m => m.Age, m => m.Rate);

        // 4. Generate yearly rows
        var rows = new List<UlipIllustrationRow>();
        decimal fv4 = 0m, fv8 = 0m;

        for (int py = 1; py <= req.PolicyTerm; py++)
        {
            int age = req.EntryAge + py - 1;   // age at start of policy year

            // AP is paid only during PPT years
            var ap = py <= req.Ppt ? req.AnnualizedPremium : 0m;

            // Premium invested = AP × (1 − PAC%)
            var premiumInvested = Round(ap * (1 - pacPct / 100m));

            // Opening FV (carried over from prior year)
            var openFv4 = fv4;
            var openFv8 = fv8;

            // --- Mortality Charge ---
            // Sum at Risk = max(SA − FV, 0)
            // MC = (SumAtRisk × MortalityRate) / 1000
            var mortalityRate = GetMortalityRate(mortalityRates, age);
            var sumAtRisk4 = Math.Max(req.SumAssured - openFv4, 0m);
            var sumAtRisk8 = Math.Max(req.SumAssured - openFv8, 0m);
            var mc4 = Round((sumAtRisk4 * mortalityRate) / 1000m);
            var mc8 = Round((sumAtRisk8 * mortalityRate) / 1000m);

            // For the row we report the average of both scenarios
            var mc = Round((mc4 + mc8) / 2m);

            // --- Policy Admin Charge ---
            var pc = Round(pcAnnual);

            // --- Net amount available for growth (4%) ---
            // Net = Opening FV + Premium Invested − MC − PC
            // Then apply FMC (deducted from fund at year end after growth)
            var net4 = openFv4 + premiumInvested - mc4 - pc;
            var net8 = openFv8 + premiumInvested - mc8 - pc;

            // Ensure net is non-negative (policy lapses otherwise — we clamp)
            net4 = Math.Max(0m, net4);
            net8 = Math.Max(0m, net8);

            // Fund growth
            fv4 = Round(net4 * (1 + AssumedReturn4));
            fv8 = Round(net8 * (1 + AssumedReturn8));

            // Apply FMC after growth
            fv4 = Round(fv4 * (1 - fmcPct / 100m));
            fv8 = Round(fv8 * (1 - fmcPct / 100m));

            // Ensure non-negative
            fv4 = Math.Max(0m, fv4);
            fv8 = Math.Max(0m, fv8);

            // Death Benefit = max(SA, FV)
            var db4 = Round(Math.Max(req.SumAssured, fv4));
            var db8 = Round(Math.Max(req.SumAssured, fv8));

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
