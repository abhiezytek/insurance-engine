using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Services;

public class BenefitCalculationService : IBenefitCalculationService
{
    private readonly InsuranceDbContext _db;

    public BenefitCalculationService(InsuranceDbContext db) => _db = db;

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
        // Resolve annualised premium (iPRM)
        var annualisedPremium = request.AnnualisedPremium ?? request.AnnualPremium;
        var frequency = request.PremiumFrequency ?? "Yearly";
        var (modalFactor, paymentsPerYear) = GetModalFactor(frequency);

        // Installment premium with loading when standard age proof is missing
        var sumAssuredForLoading = request.SumAssured ?? Round(10m * annualisedPremium);
        var installmentPremium = Round(annualisedPremium * modalFactor
            + modalFactor * (request.StandardAgeProof ? 0m : 1.5m * sumAssuredForLoading / 1000m), 0);

        // Annual premium payable across the year (installment × number of payments)
        var annualPremiumPayable = Round(installmentPremium * paymentsPerYear);

        var ppt = request.Ppt;
        var pt = request.PolicyTerm;
        var option = NormalizeOption(request.Option);
        var entryAge = request.EntryAge;

        // Life Assured age is always the driver for age-based lookups.

        // GMB factor lookup (age-specific, option-specific)
        var gmbFactor = await LookupGmbFactorAsync(ppt, pt, entryAge, option);
        var maturityBenefit = Round(annualisedPremium * gmbFactor);

        // SA on death = 10 × annualised premium (unless explicitly overridden)
        var sad = request.SumAssured ?? Round(10m * annualisedPremium);

        // Load factor tables up front
        var gsvFactors = await _db.GsvFactors.Where(x => x.Ppt == ppt && x.Pt == pt).ToListAsync();
        var ssvFactors = await _db.SsvFactors.Where(x => x.Ppt == ppt && x.Pt == pt && x.Option == option).ToListAsync();
        var loyaltyFactors = await _db.LoyaltyFactors.Where(x => x.Ppt == ppt).ToListAsync();
        var deferredFactors = option == "Deferred"
            ? await _db.DeferredIncomeFactors.Where(x => x.Ppt == ppt && x.Pt == pt).ToListAsync()
            : new List<Models.DeferredIncomeFactor>();

        // Twin years
        var twinYears = GetTwinYears(ppt, pt);

        // 8. Generate yearly rows
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
            var totalPremiumsPaid = Round(annualisedPremium * paidInstallments);

            // GI base (full, before reduction)
            decimal giBase = ComputeGiBase(py, ppt, annualisedPremium, option, deferredFactors, twinYears);

            // LI base (full, before reduction)
            decimal liBase = option == "Immediate"
                ? ComputeLiBase(py, annualisedPremium, loyaltyFactors)
                : 0m;

            // Apply paid-up reduction
            decimal gi = Round(giBase * paidUpRatio);
            decimal li = Round(liBase * paidUpRatio);

            decimal totalIncome = Round(gi + li);
            cumulativeGuaranteed = Round(cumulativeGuaranteed + gi);
            cumulativeLoyalty = Round(cumulativeLoyalty + li);

            // GSV
            var gsvRatio = LookupGsvFactor(py, gsvFactors); // CSV stores ratios directly (e.g., 0.35 = 35%)
            var gsv = Math.Max(0m, Round(totalPremiumsPaid * gsvRatio - cumulativeGuaranteed - cumulativeLoyalty));

            // Paid-up maturity benefit
            var paidUpMaturity = Round(paidUpRatio * maturityBenefit);

            // SSV
            var (ssvF1, ssvF2) = LookupSsvFactors(py, ssvFactors); // CSV values already decimal (no /100)
            // benefit-at-inception component
            var incomeComponentBase = option == "Immediate"
                ? giBase + liBase
                : giBase;
            var benefitAtInceptionComponent = Round(paidUpRatio * incomeComponentBase);
            var ssv = Math.Max(0m, Round(ssvF1 * paidUpMaturity + ssvF2 * benefitAtInceptionComponent));

            // SV
            var sv = Math.Max(0m, Math.Max(gsv, ssv));

            // Death benefit = Max(SAD, SV, 105% × Total Premiums Paid)
            var deathBenefit = Round(Math.Max(sad, Math.Max(sv, 1.05m * annualisedPremium * paidInstallments)));

            // Maturity benefit
            var maturityBenefitRow = py == pt ? paidUpMaturity : 0m;

            rows.Add(new BenefitIllustrationRow
            {
                PolicyYear = py,
                AnnualPremium = Round(annualPremiumRow),
                TotalPremiumsPaid = Round(totalPremiumsPaid),
                GuaranteedIncome = gi,
                LoyaltyIncome = li,
                TotalIncome = totalIncome,
                CumulativeSurvivalBenefits = Round(cumulativeGuaranteed + cumulativeLoyalty),
                Gsv = gsv,
                Ssv = ssv,
                SurrenderValue = sv,
                DeathBenefit = deathBenefit,
                MaturityBenefit = Round(maturityBenefitRow),
                IsPaidUp = isReducedPaidUp
            });
        }

        // 9. Max loan amount
        var lastSv = rows.Count > 0 ? rows[^1].SurrenderValue : 0m;
        var maxLoan = Round(0.70m * lastSv);

        return new BenefitIllustrationResponse
        {
            AnnualisedPremium = Round(annualisedPremium),
            AnnualPremium = Round(annualPremiumPayable),
            Ppt = ppt,
            PolicyTerm = pt,
            EntryAge = entryAge,
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

    private async Task<decimal> LookupGmbFactorAsync(int ppt, int pt, int entryAge, string option)
    {
        // Exact match
        var exact = await _db.GmbFactors.FirstOrDefaultAsync(x =>
            x.Ppt == ppt && x.Pt == pt &&
            x.EntryAgeMin <= entryAge && x.EntryAgeMax >= entryAge &&
            x.Option == option);
        if (exact != null) return exact.Factor;

        // Fallback: same PPT/PT/Option, first row
        var fallback = await _db.GmbFactors.FirstOrDefaultAsync(x =>
            x.Ppt == ppt && x.Pt == pt && x.Option == option);
        if (fallback != null) return fallback.Factor;

        throw new InvalidOperationException($"GMB factor not found for PPT={ppt}, PT={pt}, Age={entryAge}, Option={option}. Ensure {CenturyIncomeFactorLoader.GmbFile} is present and loaded.");
    }

    /// <summary>
    /// Returns the exact Twin Income payout years per product circular.
    /// PPT 7 / PT 15:  years 5, 6, 10, 11
    /// PPT 10 / PT 20: years 8, 9, 14, 15
    /// PPT 12 / PT 25: years 10, 11, 15, 16, 20, 21
    /// PPT 15 / PT 25: years 13, 14, 18, 19, 23, 24
    /// All other combinations: 2 years before PPT end and 3 years after PPT end.
    /// </summary>
    private static HashSet<int> GetTwinYears(int ppt, int pt)
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

    private static decimal ComputeGiBase(int py, int ppt, decimal ap, string option,
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

    private static decimal LookupDeferredRate(int py, List<Models.DeferredIncomeFactor> factors)
    {
        var exact = factors.FirstOrDefault(x => x.PolicyYear == py);
        if (exact != null) return exact.RatePercent;

        // Nearest
        var nearest = factors.MinBy(x => Math.Abs(x.PolicyYear - py));
        return nearest?.RatePercent ?? 0m;
    }

    private static decimal ComputeLiBase(int py, decimal ap, List<Models.LoyaltyFactor> loyaltyFactors)
    {
        if (py < 2) return 0m;
        var factor = loyaltyFactors.FirstOrDefault(x =>
            x.PolicyYearFrom <= py && (x.PolicyYearTo == null || x.PolicyYearTo >= py));
        if (factor == null) return 0m;
        return ap * factor.RatePercent / 100m;
    }

    private static decimal LookupGsvFactor(int py, List<Models.GsvFactor> factors)
    {
        var exact = factors.FirstOrDefault(x => x.PolicyYear == py);
        if (exact != null) return exact.FactorPercent;

        var nearest = factors.MinBy(x => Math.Abs(x.PolicyYear - py));
        return nearest?.FactorPercent ?? 0m;
    }

    private static (decimal f1, decimal f2) LookupSsvFactors(int py, List<Models.SsvFactor> factors)
    {
        var exact = factors.FirstOrDefault(x => x.PolicyYear == py);
        if (exact != null) return (exact.Factor1, exact.Factor2);

        var nearest = factors.MinBy(x => Math.Abs(x.PolicyYear - py));
        return nearest != null ? (nearest.Factor1, nearest.Factor2) : (0m, 0m);
    }

    private static decimal Round(decimal value, int digits = 2) =>
        Math.Round(value, digits, MidpointRounding.AwayFromZero);

    private static string NormalizeOption(string option) =>
        CenturyIncomeFactorLoader.NormalizeOption(option);
}
