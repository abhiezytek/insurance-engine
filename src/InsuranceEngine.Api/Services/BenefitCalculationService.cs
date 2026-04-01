using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InsuranceEngine.Api.Services;

public record FormulaExecutionContext(
    string ProductCode,
    string? ProductVersion,
    string? FactorVersion,
    string? FormulaVersion,
    string Option);

public interface IBenefitFormulaStrategy
{
    Task<BenefitIllustrationResponse> CalculateAsync(BenefitIllustrationRequest request, FormulaExecutionContext ctx);
}

public class BenefitCalculationService : IBenefitCalculationService
{
    internal const decimal NonStandardAgeProofLoadingRate = 1.5m;
    private readonly InsuranceDbContext _db;
    private readonly IBenefitFormulaStrategy _strategy;

    public BenefitCalculationService(InsuranceDbContext db) : this(db, new DefaultBenefitFormulaStrategy(db)) { }

    public BenefitCalculationService(InsuranceDbContext db, IMemoryCache cache)
        : this(db, new DefaultBenefitFormulaStrategy(db, cache)) { }

    public BenefitCalculationService(InsuranceDbContext db, IBenefitFormulaStrategy strategy)
    {
        _db = db;
        _strategy = strategy;
    }

    /// <summary>
    /// Modal factor lookup: fraction of annualised premium due per installment.
    /// Yearly=1.0, Half Yearly=0.5108, Quarterly=0.2582, Monthly=0.0867.
    /// </summary>
    internal static (decimal ModalFactor, int PaymentsPerYear) GetModalFactor(string premiumFrequency)
    {
        return premiumFrequency switch
        {
            "Half Yearly" or "HalfYearly" => (0.5108m, 2),
            "Quarterly" => (0.2582m, 4),
            "Monthly" => (0.0867m, 12),
            _ => (1.0m, 1) // Yearly
        };
    }

    /// <summary>
    /// Compute Annual Premium from Annualised Premium and payment frequency.
    /// Annual Premium = Annualised Premium × Modal Factor × Payments Per Year.
    /// </summary>
    internal static decimal ComputeAnnualPremium(decimal annualisedPremium, string premiumFrequency)
    {
        var (modalFactor, payments) = GetModalFactor(premiumFrequency);
        return Round(annualisedPremium * modalFactor * payments);
    }

    public async Task<BenefitIllustrationResponse> CalculateAsync(BenefitIllustrationRequest request)
    {
        var ctx = new FormulaExecutionContext(
            request.ProductCode,
            request.ProductVersion,
            request.FactorVersion ?? "table-default",
            request.FormulaVersion ?? request.ProductVersion ?? "v-default",
            NormalizeOption(request.Option));

        return await _strategy.CalculateAsync(request, ctx);
    }

    private async Task<decimal> LookupGmbFactorAsync(int ppt, int pt, int lifeAssuredAge, string option)
    {
        // Exact match
        var exact = await _db.GmbFactors.FirstOrDefaultAsync(x =>
            x.Ppt == ppt && x.Pt == pt &&
            x.EntryAgeMin <= lifeAssuredAge && x.EntryAgeMax >= lifeAssuredAge &&
            x.Option == option);
        if (exact != null) return exact.Factor;

        // Fallback: same PPT/PT/Option, first row
        var fallback = await _db.GmbFactors.FirstOrDefaultAsync(x =>
            x.Ppt == ppt && x.Pt == pt && x.Option == option);
        if (fallback != null) return fallback.Factor;

        throw new ProductConfigurationException($"GMB factor not found for PPT={ppt}, PT={pt}, LifeAssuredAge={lifeAssuredAge}, Option={option}. Ensure {CenturyIncomeFactorLoader.GmbFile} exists under the /docs folder and run the data seed/migrations.");
    }

    /// <summary>
    /// Returns the exact Twin Income payout years per product circular.
    /// PPT 7 / PT 15:  years 5, 6, 10, 11
    /// PPT 10 / PT 20: years 8, 9, 14, 15
    /// PPT 12 / PT 25: years 10, 11, 15, 16, 20, 21
    /// PPT 15 / PT 25: years 13, 14, 18, 19, 23, 24
    /// All other combinations: 2 years before PPT end and 3 years after PPT end.
    /// </summary>
    internal static HashSet<int> GetTwinYears(int ppt, int pt)
    {
        // Exact product-circular mapping
        HashSet<int>? exact = (ppt, pt) switch
        {
            (7,  15) => new HashSet<int> { 5, 6, 10, 11 },
            (10, 20) => new HashSet<int> { 8, 9, 14, 15 },
            (12, 25) => new HashSet<int> { 10, 11, 15, 16, 20, 21 },
            (15, 25) => new HashSet<int> { 13, 14, 18, 19, 23, 24 },
            _ => null
        };
        if (exact != null) return exact;

        // Fallback: generic two-pair heuristic
        var firstPairStart = Math.Max(1, ppt - 2);
        var secondPairStart = ppt + 3;
        var years = new HashSet<int>();
        foreach (var y in new[] { firstPairStart, firstPairStart + 1, secondPairStart, secondPairStart + 1 })
        {
            if (y >= 1 && y <= pt) years.Add(y);
        }
        return years;
    }

    internal static decimal ComputeGiBase(int py, int ppt, decimal ap, string option,
        List<Models.DeferredIncomeFactor> deferredFactors, HashSet<int> twinYears)
    {
        return option switch
        {
            "Immediate" => 0.10m * ap,
            "Deferred" => py <= ppt ? 0m : LookupDeferredRate(py, deferredFactors) / 100m * ap,
            "Twin" => twinYears.Contains(py) ? 1.05m * ap : 0m,
            _ => 0m
        };
    }

    internal static decimal LookupDeferredRate(int py, List<Models.DeferredIncomeFactor> factors)
    {
        var exact = factors.FirstOrDefault(x => x.PolicyYear == py);
        if (exact != null) return exact.RatePercent;

        // Nearest
        var nearest = factors.MinBy(x => Math.Abs(x.PolicyYear - py));
        return nearest?.RatePercent ?? 0m;
    }

    internal static decimal ComputeLiBase(int py, decimal ap, List<Models.LoyaltyFactor> loyaltyFactors)
    {
        if (py < 2) return 0m;
        var factor = loyaltyFactors.FirstOrDefault(x =>
            x.PolicyYearFrom <= py && (x.PolicyYearTo == null || x.PolicyYearTo >= py));
        if (factor == null) return 0m;
        return ap * factor.RatePercent / 100m;
    }

    internal static decimal LookupGsvFactor(int py, List<Models.GsvFactor> factors)
    {
        if (factors.Count == 0)
            throw new ProductConfigurationException("GSV factor table is empty. Ensure Century Income factors are seeded.");

        var exact = factors.FirstOrDefault(x => x.PolicyYear == py);
        if (exact != null) return exact.FactorPercent;

        var nearest = factors.MinBy(x => Math.Abs(x.PolicyYear - py));
        return nearest?.FactorPercent ?? throw new ProductRuleNotFoundException($"No GSV factor found for policy year {py}.", isConfigGap: true);
    }

    internal static (decimal f1, decimal f2) LookupSsvFactors(int py, List<Models.SsvFactor> factors)
    {
        if (factors.Count == 0)
            throw new ProductConfigurationException("SSV factor table is empty. Ensure Century Income SSV factors are seeded.");

        var exact = factors.FirstOrDefault(x => x.PolicyYear == py);
        if (exact != null) return (exact.Factor1, exact.Factor2);

        var nearest = factors.MinBy(x => Math.Abs(x.PolicyYear - py));
        if (nearest != null) return (nearest.Factor1, nearest.Factor2);
        throw new ProductRuleNotFoundException($"No SSV factor found for policy year {py}.", isConfigGap: true);
    }

    internal static bool HasLoyaltyIncome(string option) => option == "Immediate";

    internal static decimal Round(decimal value, int digits = 2) =>
        Math.Round(value, digits, MidpointRounding.AwayFromZero);

    internal static string NormalizeOption(string option) =>
        CenturyIncomeFactorLoader.NormalizeOption(option);
}

/// <summary>
/// Default strategy that preserves existing Century Income calculations while allowing
/// product/version-aware hooks via the FormulaExecutionContext.
/// </summary>
public class DefaultBenefitFormulaStrategy : IBenefitFormulaStrategy
{
    private readonly InsuranceDbContext _db;
    private readonly IMemoryCache _cache;

    public DefaultBenefitFormulaStrategy(InsuranceDbContext db, IMemoryCache? cache = null)
    {
        _db = db;
        _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
    }

    public async Task<BenefitIllustrationResponse> CalculateAsync(BenefitIllustrationRequest request, FormulaExecutionContext ctx)
    {
        // Resolve annualised premium (iPRM)
        var annualisedPremium = request.AnnualisedPremium ?? request.AnnualPremium;
        var frequency = request.PremiumFrequency ?? "Yearly";
        var (modalFactor, paymentsPerYear) = BenefitCalculationService.GetModalFactor(frequency);

        // Installment premium with loading when standard age proof is missing
        var sumAssuredForLoading = BenefitCalculationService.Round(10m * annualisedPremium);
        var installmentPremium = BenefitCalculationService.Round(annualisedPremium * modalFactor
            + modalFactor * (request.StandardAgeProof ? 0m : BenefitCalculationService.NonStandardAgeProofLoadingRate * sumAssuredForLoading / 1000m), 0);

        // Annual premium payable across the year (installment × number of payments)
        var annualPremiumPayable = BenefitCalculationService.Round(installmentPremium * paymentsPerYear);

        var ppt = request.Ppt;
        var pt = request.PolicyTerm;
        var option = ctx.Option;
        var lifeAssuredAge = request.EntryAge;

        // GMB factor lookup (age-specific, option-specific).
        // Guaranteed Maturity Benefit is linked to Annualised Premium per product wording.
        var gmbFactor = await LookupGmbFactorAsync(ppt, pt, lifeAssuredAge, option);
        var maturityBenefit = BenefitCalculationService.Round(annualisedPremium * gmbFactor);

        // SA on death = 10 × Annual Premium (per product wording: Sum Assured on Death
        // is linked to Annual Premium, which includes modal/frequency loading).
        var sad = BenefitCalculationService.Round(10m * annualPremiumPayable);

        // Load factor tables up front (cached for 15 minutes)
        var gsvFactors = await GetCachedAsync(
            $"gsv_factors_{ppt}_{pt}",
            () => _db.GsvFactors.AsNoTracking().Where(x => x.Ppt == ppt && x.Pt == pt).ToListAsync());

        var ssvFactors = await GetCachedAsync(
            $"ssv_factors_{ppt}_{pt}_{option}",
            () => _db.SsvFactors.AsNoTracking().Where(x => x.Ppt == ppt && x.Pt == pt && x.Option == option).ToListAsync());

        var loyaltyFactors = await GetCachedAsync(
            $"loyalty_factors_{ppt}",
            () => _db.LoyaltyFactors.AsNoTracking().Where(x => x.Ppt == ppt).ToListAsync());

        var deferredFactors = option == "Deferred"
            ? await GetCachedAsync(
                $"deferred_factors_{ppt}_{pt}",
                () => _db.DeferredIncomeFactors.AsNoTracking().Where(x => x.Ppt == ppt && x.Pt == pt).ToListAsync())
            : new List<Models.DeferredIncomeFactor>();

        // Twin years
        var twinYears = BenefitCalculationService.GetTwinYears(ppt, pt);

        // Generate yearly rows
        var rows = new List<BenefitIllustrationRow>();
        decimal cumulativeGuaranteed = 0m;
        decimal cumulativeLoyalty = 0m;

        for (int py = 1; py <= pt; py++)
        {
            var declaredPaid = request.PremiumsPaid ?? ppt;
            var isReducedPaidUp = declaredPaid < ppt;
            var paidInstallments = isReducedPaidUp ? declaredPaid : Math.Min(py, ppt);
            var paidUpRatio = isReducedPaidUp ? (decimal)declaredPaid / ppt : 1m;

            var annualPremiumRow = py <= ppt ? annualPremiumPayable : 0m;
            var totalPremiumsPaid = BenefitCalculationService.Round(annualisedPremium * paidInstallments);

            // GI base (full, before reduction)
            decimal giBase = BenefitCalculationService.ComputeGiBase(py, ppt, annualisedPremium, option, deferredFactors, twinYears);

            // LI base (full, before reduction)
            decimal liBase = BenefitCalculationService.HasLoyaltyIncome(option)
                ? BenefitCalculationService.ComputeLiBase(py, annualisedPremium, loyaltyFactors)
                : 0m;

            // Apply paid-up reduction
            decimal gi = BenefitCalculationService.Round(giBase * paidUpRatio);
            decimal li = BenefitCalculationService.Round(liBase * paidUpRatio);

            decimal totalIncome = BenefitCalculationService.Round(gi + li);
            cumulativeGuaranteed = BenefitCalculationService.Round(cumulativeGuaranteed + gi);
            cumulativeLoyalty = BenefitCalculationService.Round(cumulativeLoyalty + li);

            // GSV
            var gsvRatio = BenefitCalculationService.LookupGsvFactor(py, gsvFactors);
            var gsv = Math.Max(0m, BenefitCalculationService.Round(totalPremiumsPaid * gsvRatio - cumulativeGuaranteed - cumulativeLoyalty));

            // Paid-up maturity benefit
            var paidUpMaturity = BenefitCalculationService.Round(paidUpRatio * maturityBenefit);

            // SSV
            var (ssvF1, ssvF2) = BenefitCalculationService.LookupSsvFactors(py, ssvFactors);
            var incomeComponentBase = BenefitCalculationService.HasLoyaltyIncome(option)
                ? giBase + liBase
                : giBase;
            var benefitAtInceptionComponent = BenefitCalculationService.Round(paidUpRatio * incomeComponentBase);
            var ssv = Math.Max(0m, BenefitCalculationService.Round(ssvF1 * paidUpMaturity + ssvF2 * benefitAtInceptionComponent));

            // SV
            var sv = Math.Max(0m, Math.Max(gsv, ssv));
            var svSource = sv == ssv ? "SSV" : "GSV";

            // Death benefit = Max(SAD, SV, 105% × Total Premiums Paid)
            var deathBenefit = BenefitCalculationService.Round(Math.Max(sad, Math.Max(sv, 1.05m * annualisedPremium * paidInstallments)));

            // Maturity benefit
            var maturityBenefitRow = py == pt ? paidUpMaturity : 0m;

            rows.Add(new BenefitIllustrationRow
            {
                PolicyYear = py,
                AnnualPremium = BenefitCalculationService.Round(annualPremiumRow),
                TotalPremiumsPaid = BenefitCalculationService.Round(totalPremiumsPaid),
                GuaranteedIncome = gi,
                LoyaltyIncome = li,
                TotalIncome = totalIncome,
                CumulativeSurvivalBenefits = BenefitCalculationService.Round(cumulativeGuaranteed + cumulativeLoyalty),
                Gsv = gsv,
                Ssv = ssv,
                SurrenderValue = sv,
                GsvFactor = gsvRatio,
                SsvFactor1 = ssvF1,
                SsvFactor2 = ssvF2,
                PaidUpMaturityBenefit = paidUpMaturity,
                PaidUpIncomeComponent = benefitAtInceptionComponent,
                SurrenderValueSource = svSource,
                DeathBenefit = deathBenefit,
                MaturityBenefit = BenefitCalculationService.Round(maturityBenefitRow),
                IsPaidUp = isReducedPaidUp
            });
        }

        var lastSv = rows.Count > 0 ? rows[^1].SurrenderValue : 0m;
        var maxLoan = BenefitCalculationService.Round(0.70m * lastSv);

        return new BenefitIllustrationResponse
        {
            ProductCode = ctx.ProductCode,
            ProductVersion = ctx.ProductVersion,
            FactorVersion = ctx.FactorVersion,
            FormulaVersion = ctx.FormulaVersion,
            AnnualisedPremium = BenefitCalculationService.Round(annualisedPremium),
            AnnualPremium = BenefitCalculationService.Round(annualPremiumPayable),
            InstallmentPremium = installmentPremium,
            ModalFactor = modalFactor,
            Ppt = ppt,
            PolicyTerm = pt,
            EntryAge = lifeAssuredAge,
            Option = option,
            Channel = request.Channel,
            PremiumFrequency = frequency,
            SumAssuredOnDeath = sad,
            SumAssuredOnMaturity = maturityBenefit,
            GuaranteedMaturityBenefit = maturityBenefit,
            MaxLoanAmount = maxLoan,
            YearlyTable = rows
        };
    }

    private async Task<decimal> LookupGmbFactorAsync(int ppt, int pt, int lifeAssuredAge, string option)
    {
        // Exact match
        var exact = await _db.GmbFactors.FirstOrDefaultAsync(x =>
            x.Ppt == ppt && x.Pt == pt &&
            x.EntryAgeMin <= lifeAssuredAge && x.EntryAgeMax >= lifeAssuredAge &&
            x.Option == option);
        if (exact != null) return exact.Factor;

        var fallback = await _db.GmbFactors.FirstOrDefaultAsync(x =>
            x.Ppt == ppt && x.Pt == pt && x.Option == option);
        if (fallback != null) return fallback.Factor;

        throw new ProductConfigurationException($"GMB factor not found for PPT={ppt}, PT={pt}, LifeAssuredAge={lifeAssuredAge}, Option={option}. Ensure {CenturyIncomeFactorLoader.GmbFile} exists under the /docs folder and run the data seed/migrations.");
    }

    private async Task<T> GetCachedAsync<T>(string cacheKey, Func<Task<T>> factory)
    {
        if (_cache.TryGetValue(cacheKey, out T? cached) && cached is not null)
            return cached;

        var result = await factory();
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
        return result;
    }
}
