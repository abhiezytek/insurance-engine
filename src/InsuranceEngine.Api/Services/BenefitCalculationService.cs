using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Services;

public class BenefitCalculationService : IBenefitCalculationService
{
    private readonly InsuranceDbContext _db;

    public BenefitCalculationService(InsuranceDbContext db) => _db = db;

    public async Task<BenefitIllustrationResponse> CalculateAsync(BenefitIllustrationRequest request)
    {
        var ap = request.AnnualPremium;
        var ppt = request.Ppt;
        var pt = request.PolicyTerm;
        var option = request.Option;
        var channel = request.Channel;
        var entryAge = request.EntryAge;
        var t = request.PremiumsPaid ?? ppt;
        var n = ppt;
        var isPaidUp = t < n;

        // 1. Look up GMB factor
        var gmbFactor = await LookupGmbFactorAsync(ppt, pt, entryAge, option);

        // 2. High Premium benefit %
        var highPremiumPct = GetHighPremiumPct(ap, option);

        // 3. Channel benefit %
        var channelPct = GetChannelPct(channel, option);

        // 4-6. Compute GMB
        var baseGmb = ap * gmbFactor;
        var gmb1 = baseGmb * (1 + highPremiumPct);
        var finalGmb = gmb1 * (1 + channelPct);
        finalGmb = Round(finalGmb);

        // 7. SAD
        var sad = Round(10m * ap);

        // Load factor tables up front
        var gsvFactors = await _db.GsvFactors.Where(x => x.Ppt == ppt).ToListAsync();
        var ssvFactors = await _db.SsvFactors.Where(x => x.Ppt == ppt).ToListAsync();
        var loyaltyFactors = await _db.LoyaltyFactors.Where(x => x.Ppt == ppt).ToListAsync();
        var deferredFactors = option == "Deferred"
            ? await _db.DeferredIncomeFactors.Where(x => x.Ppt == ppt && x.Pt == pt).ToListAsync()
            : new List<Models.DeferredIncomeFactor>();

        // Twin years
        var twinYears = GetTwinYears(ppt, pt);

        // 8. Generate yearly rows
        var rows = new List<BenefitIllustrationRow>();
        decimal cumulativeSurvivalBenefits = 0m;
        var reductionRatio = isPaidUp ? (decimal)t / n : 1m;

        for (int py = 1; py <= pt; py++)
        {
            var annualPremiumRow = py <= ppt ? ap : 0m;
            var totalPremiumsPaid = Math.Min(py, ppt) * ap;

            // GI base (full, before reduction)
            decimal giBase = ComputeGiBase(py, ppt, ap, option, deferredFactors, twinYears);

            // LI base (full, before reduction)
            decimal liBase = ComputeLiBase(py, ap, loyaltyFactors);

            // Apply paid-up reduction
            decimal gi = Round(giBase * reductionRatio);
            decimal li = Round(liBase * reductionRatio);

            decimal totalIncome = Round(gi + li);
            cumulativeSurvivalBenefits = Round(cumulativeSurvivalBenefits + totalIncome);

            // GSV
            var gsvPct = LookupGsvFactor(py, gsvFactors);
            var gsv = Math.Max(0m, Round((gsvPct / 100m) * totalPremiumsPaid - cumulativeSurvivalBenefits));

            // PaidUpGMB
            var paidUpGmb = isPaidUp ? Round(reductionRatio * finalGmb) : finalGmb;

            // SSV
            var (ssvF1, ssvF2) = LookupSsvFactors(py, ssvFactors);
            var ssvIncomeComponent = Round(giBase * reductionRatio + li);
            var ssv = Math.Max(0m, Round((ssvF1 / 100m) * paidUpGmb + (ssvF2 / 100m) * ssvIncomeComponent));

            // SV
            var sv = Math.Max(0m, Math.Max(gsv, ssv));

            // Death benefit
            var deathBenefit = Round(Math.Max(10m * ap, Math.Max(sv, 1.05m * totalPremiumsPaid)));

            // Maturity benefit
            var maturityBenefit = py == pt ? finalGmb : 0m;

            rows.Add(new BenefitIllustrationRow
            {
                PolicyYear = py,
                AnnualPremium = Round(annualPremiumRow),
                TotalPremiumsPaid = Round(totalPremiumsPaid),
                GuaranteedIncome = gi,
                LoyaltyIncome = li,
                TotalIncome = totalIncome,
                CumulativeSurvivalBenefits = cumulativeSurvivalBenefits,
                Gsv = gsv,
                Ssv = ssv,
                SurrenderValue = sv,
                DeathBenefit = deathBenefit,
                MaturityBenefit = Round(maturityBenefit),
                IsPaidUp = isPaidUp
            });
        }

        // 9. Max loan amount
        var lastSv = rows.Count > 0 ? rows[^1].SurrenderValue : 0m;
        var maxLoan = Round(0.70m * lastSv);

        return new BenefitIllustrationResponse
        {
            AnnualPremium = Round(ap),
            Ppt = ppt,
            PolicyTerm = pt,
            EntryAge = entryAge,
            Option = option,
            Channel = channel,
            SumAssuredOnDeath = sad,
            GuaranteedMaturityBenefit = finalGmb,
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

        return 11.5m;
    }

    private static decimal GetHighPremiumPct(decimal ap, string option)
    {
        if (ap >= 200000m)
        {
            return option switch
            {
                "Immediate" => 0.035m,
                "Deferred" => 0.030m,
                "Twin" => 0.045m,
                _ => 0m
            };
        }
        if (ap >= 100000m)
        {
            return option switch
            {
                "Immediate" => 0.03m,
                "Deferred" => 0.0225m,
                "Twin" => 0.0325m,
                _ => 0m
            };
        }
        return 0m;
    }

    private static decimal GetChannelPct(string channel, string option)
    {
        return channel switch
        {
            "Online" => option switch
            {
                "Immediate" => 0.0425m,
                "Deferred" => 0.035m,
                "Twin" => 0.0425m,
                _ => 0m
            },
            "StaffDirect" => option switch
            {
                "Immediate" => 0.085m,
                "Deferred" => 0.07m,
                "Twin" => 0.085m,
                _ => 0m
            },
            _ => 0m
        };
    }

    private static HashSet<int> GetTwinYears(int ppt, int pt)
    {
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

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
