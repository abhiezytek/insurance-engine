using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Threading;

namespace InsuranceEngine.Api.Services;

/// <summary>
/// ULIP Benefit Illustration calculation service for SUD Life e-Wealth Royale.
///
/// Implements a month-by-month projection engine aligned with the product's
/// File &amp; Use document and IRDAI ULIP disclosure requirements.
///
/// Key product rules implemented:
/// - PAC: ₹100/month for first 10 years.
/// - FMC: 0.1118% per month (effective; used for Self-Managed strategy).
/// - Monthly gross return: effective monthly = (1 + annual_rate)^(1/12) − 1.
/// - Mortality: monthly MC = SAR × annual_rate[age] / 12000.
///   SAR = max(SA − FV, 105% × TPP − FV, 0).
/// - Loyalty Addition: 0.10% × avg(last 12 months' FV) at end of years 6..PPT.
/// - Wealth Booster: 3% × avg(last 24 months' FV) at end of years 10, 15, 20, ...
/// - RoPAC: total PAC paid, credited at end of year 10.
/// - RoMC: total MC paid, credited at maturity.
/// - Surrender Value: FV − Discontinuance Charge (years 1–4, IRDAI tiered schedule).
/// - Death Benefit: max(SA, FV, 105% × total premiums paid).
/// - No intermediate rounding; results rounded for output display only.
/// </summary>
public class UlipCalculationService : IUlipCalculationService
{
    // -----------------------------------------------------------------------
    // Product constants — align with File & Use / workbook
    // -----------------------------------------------------------------------

    /// <summary>Premium Allocation Charge (%): 0% — full AP is invested.</summary>
    private const decimal PacPercent = 0m;

    /// <summary>
    /// Policy Administration Charge (PolicyAdminCharge) per month in rupees (first 10 years only).
    /// Note: "PAC" in the IRDAI Part B column refers to "Premium Allocation Charge" (= 0% here);
    /// the Policy Administration Charge is a separate ₹100/month deduction.
    /// </summary>
    private const decimal PolicyAdminChargeMonthly = 100m;

    /// <summary>Policy year through which PolicyAdminCharge is charged.</summary>
    private const int PolicyAdminChargeEndYear = 10;

    /// <summary>Default FMC per month (0.1118%) used when CSV master is unavailable.</summary>
    private const decimal DefaultFmcMonthly = 0.001118m;

    private static readonly Lazy<Dictionary<string, decimal>> CsvFmcMonthlyRates = new(LoadFmcMonthlyRates, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<Dictionary<string, Dictionary<int, decimal>>> CsvMortalityRates = new(LoadMortalityRates, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Loyalty Addition rate: 0.10% of average 12-month fund value.</summary>
    private const decimal LoyaltyAdditionRate = 0.001m;

    /// <summary>Wealth Booster rate: 3% of average 24-month fund value.</summary>
    private const decimal WealthBoosterRate = 0.03m;

    /// <summary>Loyalty Addition credited from this policy year (inclusive).</summary>
    private const int LoyaltyStartYear = 6;

    /// <summary>Wealth Booster milestone cadence (every N-th year starting from year 10).</summary>
    private const int WealthBoosterStartYear = 10;
    private const int WealthBoosterCadence = 5;

    /// <summary>Return of PAC at end of this policy year.</summary>
    private const int RoPacYear = 10;

    private readonly InsuranceDbContext _db;

    public UlipCalculationService(InsuranceDbContext db) => _db = db;

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    public async Task<UlipCalculationResponse> CalculateAsync(UlipCalculationRequest req)
    {
        var effectiveDate = req.PolicyEffectiveDate ?? DateTime.UtcNow;

        // Life Assured same as Policyholder → mirror fields for validation/calculation
        if (req.LifeAssuredSameAsPolicyholder)
        {
            req.PolicyholderName = string.IsNullOrWhiteSpace(req.PolicyholderName) ? req.CustomerName : req.PolicyholderName;
            req.PolicyholderGender = string.IsNullOrWhiteSpace(req.PolicyholderGender) ? req.Gender : req.PolicyholderGender;
            req.PolicyholderDateOfBirth = req.PolicyholderDateOfBirth == default ? req.DateOfBirth : req.PolicyholderDateOfBirth;
            req.PolicyholderAge = req.PolicyholderAge == 0 ? req.EntryAge : req.PolicyholderAge;
        }

        // Derive EntryAge from DOB when not explicitly provided (UI should already
        // send DOB per correction prompt; EntryAge retained for backward compatibility).
        if (req.EntryAge <= 0 && req.DateOfBirth != default)
        {
            var age = effectiveDate.Year - req.DateOfBirth.Year;
            if (req.DateOfBirth.Date > effectiveDate.AddYears(-age)) age--;
            req.EntryAge = Math.Max(0, age);
        }

        // Validate PPT/PT combination using CSV rulebook.
        var pptValidation = PptPtRuleBook.Validate(req, req.EntryAge, effectiveDate);
        if (!pptValidation.IsValid)
            throw new InvalidOperationException(pptValidation.Error);

        var riskPrefValidation = RiskPreferenceRuleBook.ValidateAndNormalize(
            req,
            () => RiskPreferenceRuleBook.HasAgeBasedAllocationMaster(_db, req.ProductCode));
        if (!riskPrefValidation.IsValid)
            throw new InvalidOperationException(riskPrefValidation.Error);

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Code == req.ProductCode && p.ProductType == "ULIP");
        var productName = product?.Name ?? req.ProductCode;

        // Derive Sum Assured from premium and PPT type (single vs regular).
        req.SumAssured = DeriveSumAssured(req);

        // Load mortality rates for the life assured's gender
        var mortalityRates = await _db.MortalityRates
            .Where(m => m.Gender == req.Gender)
            .ToDictionaryAsync(m => m.Age, m => m.Rate);

        // Determine months-since-last-birthday at policy commencement
        // (used to correctly shift the age in each month of the projection)
        var birthMonth = req.DateOfBirth != default ? req.DateOfBirth.Month : effectiveDate.Month;
        var monthsSinceLastBirthday = ((effectiveDate.Month - birthMonth) % 12 + 12) % 12;

        // Resolve FMC (weighted by fund allocations if provided)
        var fmcMonthly = ResolveFmcMonthly(req);

        // Run month-by-month projection for both rate scenarios
        var rows4 = RunMonthlyProjection(req, 0.04m, mortalityRates, monthsSinceLastBirthday, fmcMonthly);
        var rows8 = RunMonthlyProjection(req, 0.08m, mortalityRates, monthsSinceLastBirthday, fmcMonthly);

        // Build output lists
        var partARows = new List<PartARow>();
        var partBRows4 = new List<PartBRow>();
        var partBRows8 = new List<PartBRow>();
        var legacyRows = new List<UlipIllustrationRow>();

        for (int py = 1; py <= req.PolicyTerm; py++)
        {
            var r4 = rows4[py - 1];
            var r8 = rows8[py - 1];
            var ap = py <= req.Ppt ? req.AnnualizedPremium : 0m;

            var partBRow4 = BuildPartBRow(py, ap, req.AnnualizedPremium, r4);
            var partBRow8 = BuildPartBRow(py, ap, req.AnnualizedPremium, r8);

            partARows.Add(new PartARow
            {
                Year = py,
                AnnualizedPremium = ap,
                MortalityCharges4 = partBRow4.MortalityCharges,
                ArbCharges4 = partBRow4.ArbCharges,
                OtherCharges4 = Round2(partBRow4.PolicyAdministrationCharges + partBRow4.FundManagementCharge),
                Gst4 = partBRow4.Gst,
                FundAtEndOfYear4 = partBRow4.FundAtEndOfYear,
                SurrenderValue4 = partBRow4.SurrenderValue,
                DeathBenefit4 = partBRow4.DeathBenefit,
                MortalityCharges8 = partBRow8.MortalityCharges,
                ArbCharges8 = partBRow8.ArbCharges,
                OtherCharges8 = Round2(partBRow8.PolicyAdministrationCharges + partBRow8.FundManagementCharge),
                Gst8 = partBRow8.Gst,
                FundAtEndOfYear8 = partBRow8.FundAtEndOfYear,
                SurrenderValue8 = partBRow8.SurrenderValue,
                DeathBenefit8 = partBRow8.DeathBenefit,
            });

            partBRows4.Add(partBRow4);
            partBRows8.Add(partBRow8);

            legacyRows.Add(new UlipIllustrationRow
            {
                Year = py,
                Age = req.EntryAge + py - 1,
                AnnualPremium = ap,
                PremiumInvested = ap * (1m - PacPercent / 100m),
                // Legacy single-value MC: use the 8% scenario (higher return → lower SAR → slightly lower MC).
                // This field exists only for backward-compatibility; consumers should use PartBRows4/8 for per-scenario accuracy.
                MortalityCharge = Round2(r8.TotalMc),
                PolicyCharge = Round2(r4.TotalPac),
                FundValue4 = Round2(r4.FundEnd),
                DeathBenefit4 = Round2(r4.DeathBenefit),
                FundValue8 = Round2(r8.FundEnd),
                DeathBenefit8 = Round2(r8.DeathBenefit),
            });
        }

        var lastR4 = rows4[req.PolicyTerm - 1];
        var lastR8 = rows8[req.PolicyTerm - 1];

        await PersistResultsAsync(req, legacyRows);

        // Calculated fields per Quick-Action-Guide specification
        var maturityAge = req.EntryAge + req.PolicyTerm;
        var premiumInstallment = CalculatePremiumInstallment(req.AnnualizedPremium, req.PremiumFrequency);
        var netYield4 = CalculateNetYield(req.AnnualizedPremium, req.Ppt, req.PolicyTerm, (decimal)lastR4.FundEnd);
        var netYield8 = CalculateNetYield(req.AnnualizedPremium, req.Ppt, req.PolicyTerm, (decimal)lastR8.FundEnd);

        return new UlipCalculationResponse
        {
            PolicyNumber       = req.PolicyNumber,
            CustomerName       = req.CustomerName,
            ProductCode        = req.ProductCode,
            ProductName        = productName,
            Option             = req.Option,
            LifeAssuredSameAsPolicyholder = req.LifeAssuredSameAsPolicyholder,
            Gender             = req.Gender,
            EntryAge           = req.EntryAge,
            PolicyTerm         = req.PolicyTerm,
            Ppt                = req.Ppt,
            AnnualizedPremium  = req.AnnualizedPremium,
            SumAssured         = req.SumAssured,
            PremiumFrequency   = req.PremiumFrequency,
            MaturityAge        = maturityAge,
            PremiumInstallment = premiumInstallment,
            NetYield4          = netYield4,
            NetYield8          = netYield8,
            MaturityBenefit4   = Round2(lastR4.FundEnd),
            MaturityBenefit8   = Round2(lastR8.FundEnd),
            PartARows          = partARows,
            PartBRows4         = partBRows4,
            PartBRows8         = partBRows8,
            YearlyTable        = legacyRows,
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
            YearlyTable       = rows,
            MaturityBenefit4  = last.FundValue4,
            MaturityBenefit8  = last.FundValue8,
        };
    }

    // -----------------------------------------------------------------------
    // Monthly projection engine
    // -----------------------------------------------------------------------

    /// <summary>
    /// Holds per-policy-year accumulated results for one rate scenario,
    /// as computed by the monthly projection loop.
    /// </summary>
    private sealed record YearlyAccumulator(
        double TotalMc,            // Annual sum of monthly mortality charges
        double TotalPac,           // Annual sum of monthly policy admin charges
        double TotalFmc,           // Annual sum of monthly FMC charges
        double FundBeforeFmc,      // Fund value at anniversary month BEFORE that month's FMC
        double LastMonthFmc,       // FMC deducted in the anniversary month
        double LoyaltyAddition,    // LA credited at this anniversary
        double WealthBooster,      // WB credited at this anniversary
        double ReturnOfCharges,    // RoPAC or RoMC credited at this anniversary
        double FundEnd,            // Fund value at end of year (after FMC + additions)
        double SurrenderValue,     // FundEnd − Discontinuance Charge
        double DeathBenefit        // max(SA, FundEnd, 105% × cumTPP)
    );

    private List<YearlyAccumulator> RunMonthlyProjection(
        UlipCalculationRequest req,
        decimal annualRate,
        Dictionary<int, decimal> mortalityRates,
        int monthsSinceLastBirthday,
        double fmcMonthly)
    {
        var policyTerm = req.PolicyTerm;
        var ppt        = req.Ppt;
        var sa         = (double)req.SumAssured;
        var ap         = (double)req.AnnualizedPremium;

        // Effective monthly gross return: (1 + annual)^(1/12) − 1
        var monthlyRate = Math.Pow(1.0 + (double)annualRate, 1.0 / 12.0) - 1.0;
        var pacMonthly  = (double)PolicyAdminChargeMonthly;

        // Installment premium for each premium month
        var installment = FrequencyInstallment(ap, req.PremiumFrequency);

        // Which month-within-year are premium months for this frequency?
        var premiumMonths = PremiumMonthsInYear(req.PremiumFrequency); // e.g. {1} for Yearly

        // Running state
        double fv          = 0.0;
        double cumTpp      = 0.0;   // cumulative total premiums paid (for 105% floor)
        double cumMc       = 0.0;   // cumulative mortality charges paid (for RoMC at maturity)
        double cumPac      = 0.0;   // cumulative policy admin charges (for RoPAC at year 10)

        // History of end-of-month fund values (for LA and WB averaging)
        var fvHistory = new List<double>();

        var yearlyResults = new List<YearlyAccumulator>();

        for (int py = 1; py <= policyTerm; py++)
        {
            double yearMc  = 0.0;
            double yearPac = 0.0;
            double yearFmc = 0.0;
            double fundBeforeFmcAtAnniv = 0.0;
            double lastMonthFmc = 0.0;

            for (int miy = 1; miy <= 12; miy++)
            {
                int globalMonth = (py - 1) * 12 + miy;

                // --- Determine current age of LA in this month ---
                int currentAge = req.EntryAge + (globalMonth - 1 + monthsSinceLastBirthday) / 12;

                // --- Premium received (if premium month and within PPT) ---
                if (py <= ppt && premiumMonths.Contains(miy))
                {
                    fv += installment;
                    cumTpp += installment;
                }

                // --- Deduct Policy Admin Charge ---
                double pac = py <= PolicyAdminChargeEndYear ? pacMonthly : 0.0;
                fv -= pac;
                yearPac += pac;
                cumPac  += pac;

                // --- Mortality Charge ---
                double sar = Math.Max(sa - fv, Math.Max(1.05 * cumTpp - fv, 0.0));
                double annualMortalityRate = GetMortalityRate(mortalityRates, currentAge, req.Gender);
                double mc = sar * annualMortalityRate / 12000.0;
                fv -= mc;
                yearMc += mc;
                cumMc  += mc;

                // --- Monthly gross return ---
                fv += fv * monthlyRate;

                // --- FMC ---
                double fmc = fv * fmcMonthly;
                if (miy == 12)
                {
                    fundBeforeFmcAtAnniv = fv;   // capture BEFORE deducting this month's FMC
                    lastMonthFmc = fmc;
                }
                fv -= fmc;
                yearFmc += fmc;

                // Record end-of-month FV (after FMC, before anniversary additions)
                fvHistory.Add(fv);
            }

            // === Policy anniversary (month 12 of this year) ===

            double la  = 0.0;
            double wb  = 0.0;
            double roc = 0.0;

            // Loyalty Addition: from year 6 up to end of PPT
            if (py >= LoyaltyStartYear && py <= ppt)
            {
                // Average of last 12 months' end-of-month FV values
                var last12 = fvHistory.Skip(fvHistory.Count - 12).Take(12).ToList();
                la = last12.Average() * (double)LoyaltyAdditionRate;
                fv += la;
            }

            // Wealth Booster: years 10, 15, 20, ...
            if (py >= WealthBoosterStartYear && (py - WealthBoosterStartYear) % WealthBoosterCadence == 0)
            {
                int take = Math.Min(24, fvHistory.Count);
                var last24 = fvHistory.Skip(fvHistory.Count - take).Take(take).ToList();
                wb = last24.Average() * (double)WealthBoosterRate;
                fv += wb;
            }

            // Return of Policy Admin Charges at year 10
            if (py == RoPacYear)
            {
                roc += cumPac;
                fv  += cumPac;
            }

            // Return of Mortality Charges at maturity
            if (py == policyTerm && policyTerm != RoPacYear)
            {
                roc += cumMc;
                fv  += cumMc;
            }
            else if (py == policyTerm && policyTerm == RoPacYear)
            {
                // Both RoPAC and RoMC at the same year (e.g., if PT=10)
                roc += cumMc;
                fv  += cumMc;
            }

            fv = Math.Max(0.0, fv);

            // Death Benefit
            double minDeathFloor = 1.05 * cumTpp;
            double db = Math.Max(sa, Math.Max(fv, minDeathFloor));

            // Surrender Value (IRDAI discontinuance charges apply years 1–4)
            double dc = GetDiscontinuanceCharge(py, ap);
            double sv = Math.Max(0.0, fv - dc);

            yearlyResults.Add(new YearlyAccumulator(
                TotalMc:           yearMc,
                TotalPac:          yearPac,
                TotalFmc:          yearFmc,
                FundBeforeFmc:     fundBeforeFmcAtAnniv,
                LastMonthFmc:      lastMonthFmc,
                LoyaltyAddition:   la,
                WealthBooster:     wb,
                ReturnOfCharges:   roc,
                FundEnd:           fv,
                SurrenderValue:    sv,
                DeathBenefit:      db
            ));
        }

        return yearlyResults;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static PartBRow BuildPartBRow(int year, decimal ap, decimal annPremium, YearlyAccumulator r)
    {
        var pac = ap * (PacPercent / 100m);
        return new PartBRow
        {
            Year = year,
            AnnualizedPremium          = ap,
            PremiumAllocationCharge    = Round2(pac),
            AnnualizedPremiumAfterPac  = Round2(ap - pac),
            MortalityCharges           = Round2((decimal)r.TotalMc),
            ArbCharges                 = 0m,
            Gst                        = 0m,
            PolicyAdministrationCharges= Round2((decimal)r.TotalPac),
            ExtraPremiumAllocation     = 0m,
            FundBeforeFmc              = Round2((decimal)r.FundBeforeFmc),
            FundManagementCharge       = Round2((decimal)r.TotalFmc),
            LoyaltyAddition            = Round2((decimal)r.LoyaltyAddition),
            WealthBooster              = Round2((decimal)r.WealthBooster),
            ReturnOfCharges            = Round2((decimal)r.ReturnOfCharges),
            FundAtEndOfYear            = Round2((decimal)r.FundEnd),
            SurrenderValue             = Round2((decimal)r.SurrenderValue),
            DeathBenefit               = Round2((decimal)r.DeathBenefit),
        };
    }

    /// <summary>Installment premium amount per payment event given frequency.</summary>
    private static double FrequencyInstallment(double annualizedPremium, string frequency) =>
        frequency switch
        {
            "Half Yearly" or "HalfYearly"  => annualizedPremium / 2.0,
            "Quarterly"                     => annualizedPremium / 4.0,
            "Monthly"                       => annualizedPremium / 12.0,
            _                               => annualizedPremium,   // Yearly / default
        };

    /// <summary>Derive Sum Assured per product rules (single pay vs regular).</summary>
    private static decimal DeriveSumAssured(UlipCalculationRequest req)
    {
        // Treat PPT=1 or TypeOfPpt="Single" as single-pay.
        var isSinglePay = req.Ppt == 1 || string.Equals(req.TypeOfPpt, "Single", StringComparison.OrdinalIgnoreCase);
        var multiplier = isSinglePay ? 1.25m : 10m;
        var basePremium = isSinglePay ? req.AnnualizedPremium : req.AnnualizedPremium;
        var derived = Round2((double)(multiplier * basePremium));
        return (decimal)derived;
    }

    /// <summary>
    /// Returns the set of month-within-year indices (1–12) on which a premium
    /// installment is due, given the payment frequency.
    /// </summary>
    private static HashSet<int> PremiumMonthsInYear(string frequency) =>
        frequency switch
        {
            "Half Yearly" or "HalfYearly" => new HashSet<int> { 1, 7 },
            "Quarterly"                    => new HashSet<int> { 1, 4, 7, 10 },
            "Monthly"                      => new HashSet<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 },
            _                              => new HashSet<int> { 1 },   // Yearly
        };

    /// <summary>
    /// IRDAI-mandated Discontinuance Charge for years 1–4
    /// (zero from year 5 onwards or once locked-in period ends).
    /// Bands follow IRDAI 2019 ULIP circular.
    /// </summary>
    private static double GetDiscontinuanceCharge(int policyYear, double ap)
    {
        if (policyYear >= 5) return 0.0;

        double dc = policyYear switch
        {
            1 => ap <= 25000 ? Math.Min(0.20 * ap, 3000) : ap <= 50000 ? Math.Min(0.06 * ap, 6000) : 6000,
            2 => ap <= 25000 ? Math.Min(0.15 * ap, 2000) : ap <= 50000 ? Math.Min(0.04 * ap, 5000) : 5000,
            3 => ap <= 25000 ? Math.Min(0.10 * ap, 1500) : ap <= 50000 ? Math.Min(0.03 * ap, 4000) : 4000,
            4 => ap <= 25000 ? Math.Min(0.05 * ap, 1000) : ap <= 50000 ? Math.Min(0.02 * ap, 2000) : 2000,
            _ => 0.0,
        };
        return dc;
    }

    private static double GetMortalityRate(Dictionary<int, decimal> rates, int age, string gender)
    {
        if (rates.TryGetValue(age, out var rate)) return (double)rate;
        if (rates.Count > 0)
        {
            var nearest = rates.MinBy(kv => Math.Abs(kv.Key - age));
            return (double)nearest.Value;
        }

        var csvRates = CsvMortalityRates.Value;
        var genderKey = NormalizeGender(gender);
        if (csvRates.TryGetValue(genderKey, out var genderRates) && genderRates.TryGetValue(age, out var csvRate))
            return (double)csvRate;

        return (double)FallbackMortalityRate(age);
    }

    /// <summary>
    /// SUD Life e-Wealth Royale product mortality rates (per 1000 per year),
    /// sourced from the uploaded commission/mortality CSV reference table.
    /// Used only when the DB mortality table is empty.
    /// </summary>
    private static decimal FallbackMortalityRate(int age) => age switch
    {
        0     => 5.01m,
        1     => 4.10m,
        2     => 1.10m,
        3     => 0.56m,
        4     => 0.33m,
        5     => 0.22m,
        6     => 0.18m,
        7     => 0.18m,
        8     => 0.20m,
        9     => 0.25m,
        10    => 0.32m,
        11    => 0.41m,
        12    => 0.52m,
        13    => 0.63m,
        14    => 0.74m,
        15    => 0.84m,
        16    => 0.92m,
        17    => 1.00m,
        18    => 1.05m,
        19    => 1.09m,
        20    => 1.11m,
        21    => 1.12m,
        22    => 1.12m,
        23    => 1.12m,
        24    => 1.12m,
        25    => 1.12m,
        26    => 1.12m,
        27    => 1.12m,
        28    => 1.13m,
        29    => 1.15m,
        30    => 1.17m,
        31    => 1.21m,
        32    => 1.25m,
        33    => 1.30m,
        34    => 1.37m,
        35    => 1.44m,
        36    => 1.53m,
        37    => 1.63m,
        38    => 1.74m,
        39    => 1.87m,
        40    => 2.02m,
        41    => 2.18m,
        42    => 2.36m,
        43    => 2.57m,
        44    => 2.81m,
        45    => 3.10m,
        46    => 3.42m,
        47    => 3.80m,
        48    => 4.24m,
        49    => 4.75m,
        50    => 5.32m,
        51    => 5.96m,
        52    => 6.66m,
        53    => 7.41m,
        54    => 8.20m,
        55    => 9.02m,
        56    => 9.85m,
        57    => 10.71m,
        58    => 11.58m,
        59    => 12.47m,
        60    => 13.39m,
        61    => 14.36m,
        62    => 15.40m,
        63    => 16.52m,
        64    => 17.75m,
        65    => 19.12m,
        66    => 20.65m,
        67    => 22.36m,
        68    => 24.29m,
        69    => 26.45m,
        70    => 28.87m,
        71    => 31.58m,
        72    => 34.60m,
        73    => 37.97m,
        74    => 41.71m,
        _     => 45.87m,
    };

    private async Task PersistResultsAsync(UlipCalculationRequest req, List<UlipIllustrationRow> rows)
    {
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

    /// <summary>
    /// Calculates the premium installment based on frequency.
    /// Premium Installment = AnnualizedPremium × ModalFactor.
    /// </summary>
    private static decimal CalculatePremiumInstallment(decimal annualizedPremium, string frequency) =>
        frequency switch
        {
            "Half Yearly" or "HalfYearly" => Math.Round(annualizedPremium * 0.5108m, 2, MidpointRounding.AwayFromZero),
            "Quarterly"                    => Math.Round(annualizedPremium * 0.2582m, 2, MidpointRounding.AwayFromZero),
            "Monthly"                      => Math.Round(annualizedPremium * 0.0867m, 2, MidpointRounding.AwayFromZero),
            _                              => annualizedPremium,   // Yearly: factor = 1.0
        };

    /// <summary>
    /// Calculates the Net Yield (approximate IRR) of the premium cash flows
    /// versus the maturity fund value.
    ///
    /// Cash flows: −AP each year for PPT years, +MaturityFV at end of PT.
    /// Uses Newton–Raphson iteration to solve for internal rate of return.
    /// </summary>
    private static decimal CalculateNetYield(decimal ap, int ppt, int pt, decimal maturityFv)
    {
        if (maturityFv <= 0 || ap <= 0 || pt <= 0) return 0m;

        double annualPremium = (double)ap;
        double fv = (double)maturityFv;

        // Newton–Raphson for IRR: Σ( CF_t / (1+r)^t ) = 0
        // DerivativeTolerance: if |f'(r)| is below this threshold the derivative is effectively zero; stop to avoid division by zero.
        const double DerivativeTolerance = 1e-12;
        // RateTolerance: stop iterating once the rate change between iterations is below this threshold (converged).
        const double RateTolerance = 1e-10;

        double r = 0.06; // initial guess
        for (int iter = 0; iter < 100; iter++)
        {
            double npv = 0;
            double dnpv = 0;
            for (int t = 1; t <= ppt; t++)
            {
                double discount = Math.Pow(1 + r, t);
                npv  -= annualPremium / discount;
                dnpv += t * annualPremium / (discount * (1 + r));
            }
            double matDiscount = Math.Pow(1 + r, pt);
            npv  += fv / matDiscount;
            dnpv -= pt * fv / (matDiscount * (1 + r));

            if (Math.Abs(dnpv) < DerivativeTolerance) break;
            double newR = r - npv / dnpv;
            if (Math.Abs(newR - r) < RateTolerance) { r = newR; break; }
            r = newR;
        }

        return Math.Round((decimal)(r * 100), 3, MidpointRounding.AwayFromZero);
    }

    private static decimal Round2(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static decimal Round2(double value) =>
        Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);

    private static double ResolveFmcMonthly(UlipCalculationRequest req)
    {
        var fmcTable = CsvFmcMonthlyRates.Value;
        if (fmcTable.Count == 0)
            return (double)DefaultFmcMonthly;

        if (req.FundAllocations != null && req.FundAllocations.Count > 0)
        {
            decimal totalPct = req.FundAllocations.Sum(f => f.AllocationPercent);
            if (totalPct > 0)
            {
                decimal weighted = 0m;
                foreach (var alloc in req.FundAllocations)
                {
                    if (!fmcTable.TryGetValue(alloc.FundType ?? string.Empty, out var fmc))
                        fmc = DefaultFmcMonthly;
                    weighted += (alloc.AllocationPercent / totalPct) * fmc;
                }
                return (double)weighted;
            }
        }

        if (fmcTable.TryGetValue("Blue-chip Equity Fund", out var defaultFmc))
            return (double)defaultFmc;

        return (double)DefaultFmcMonthly;
    }

    private static Dictionary<string, decimal> LoadFmcMonthlyRates()
    {
        var path = FindDocFile("ewealth_fmc_factors.csv");
        if (path == null) return new(StringComparer.OrdinalIgnoreCase);

        var dict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',', StringSplitOptions.TrimEntries);
            if (cols.Length < 3) continue;
            if (decimal.TryParse(cols[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var fmcPm))
                dict[cols[0]] = fmcPm;
        }
        return dict;
    }

    private static Dictionary<string, Dictionary<int, decimal>> LoadMortalityRates()
    {
        var path = FindDocFile("ewealth_mortality_factors.csv");
        var dict = new Dictionary<string, Dictionary<int, decimal>>(StringComparer.OrdinalIgnoreCase)
        {
            ["male"] = new(),
            ["female"] = new(),
            ["transgender"] = new(),
        };
        if (path == null) return dict;

        foreach (var line in File.ReadAllLines(path).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',', StringSplitOptions.TrimEntries);
            if (cols.Length < 3) continue;
            if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var age)) continue;

            // CSV columns: age_last_birthday, male_rate, transgender_rate, female_rate, atpd_rate
            if (decimal.TryParse(cols.ElementAtOrDefault(1), NumberStyles.Number, CultureInfo.InvariantCulture, out var male))
                dict["male"][age] = male;
            if (decimal.TryParse(cols.ElementAtOrDefault(2), NumberStyles.Number, CultureInfo.InvariantCulture, out var trans))
                dict["transgender"][age] = trans;
            if (decimal.TryParse(cols.ElementAtOrDefault(3), NumberStyles.Number, CultureInfo.InvariantCulture, out var female))
                dict["female"][age] = female;
        }

        return dict;
    }

    private static string NormalizeGender(string gender) =>
        string.IsNullOrWhiteSpace(gender) ? "male" : gender.Trim().ToLowerInvariant();

    private static string? FindDocFile(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "docs", fileName);
            if (File.Exists(candidate)) return candidate;
            var direct = Path.Combine(current.FullName, fileName);
            if (File.Exists(direct)) return direct;
            current = current.Parent;
        }

        return null;
    }
}
