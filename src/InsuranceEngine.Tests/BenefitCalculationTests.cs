using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InsuranceEngine.Tests;

[TestFixture]
public class BenefitCalculationTests
{
    private BenefitCalculationService _svc = null!;
    private InsuranceDbContext _db = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase("BenefitCalcTestDb_" + Guid.NewGuid())
            .Options;
        _db = new InsuranceDbContext(options);

        // Seed GMB factors
        _db.GmbFactors.AddRange(
            new GmbFactor { Id=1,  Ppt=7,  Pt=15, EntryAgeMin=0,  EntryAgeMax=40, Option="Immediate", Factor=11.50m },
            new GmbFactor { Id=2,  Ppt=7,  Pt=15, EntryAgeMin=41, EntryAgeMax=65, Option="Immediate", Factor=10.80m },
            new GmbFactor { Id=3,  Ppt=7,  Pt=15, EntryAgeMin=0,  EntryAgeMax=40, Option="Deferred",  Factor=12.50m },
            new GmbFactor { Id=4,  Ppt=7,  Pt=15, EntryAgeMin=41, EntryAgeMax=65, Option="Deferred",  Factor=11.80m },
            new GmbFactor { Id=5,  Ppt=7,  Pt=15, EntryAgeMin=0,  EntryAgeMax=40, Option="Twin",      Factor=13.00m },
            new GmbFactor { Id=6,  Ppt=7,  Pt=15, EntryAgeMin=41, EntryAgeMax=65, Option="Twin",      Factor=12.30m },
            new GmbFactor { Id=7,  Ppt=10, Pt=20, EntryAgeMin=0,  EntryAgeMax=40, Option="Immediate", Factor=12.00m },
            new GmbFactor { Id=8,  Ppt=10, Pt=20, EntryAgeMin=41, EntryAgeMax=65, Option="Immediate", Factor=11.20m }
        );

        // Seed GSV factors for PPT=7, PT=15
        decimal[] gsv7_15 = { 0,35,35,50,50,50,50,55.71m,61.43m,67.14m,72.86m,78.57m,84.29m,90,90 };
        for (int i = 0; i < gsv7_15.Length; i++)
            _db.GsvFactors.Add(new GsvFactor { Id=100+i, Ppt=7, Pt=15, PolicyYear=i+1, FactorPercent=gsv7_15[i] });

        // Seed GSV factors for PPT=7, PT=20 (different from PT=15 starting at PY8)
        decimal[] gsv7_20 = { 0,35,35,50,50,50,50,53.33m,56.67m,60,63.33m,66.67m,70,73.33m,76.67m,80,83.33m,86.67m,90,90 };
        for (int i = 0; i < gsv7_20.Length; i++)
            _db.GsvFactors.Add(new GsvFactor { Id=150+i, Ppt=7, Pt=20, PolicyYear=i+1, FactorPercent=gsv7_20[i] });

        // Seed SSV factors for PPT=7, PT=15, Immediate
        decimal[] ssv7_15_f1   = { 0,37.13m,39.82m,42.70m,45.80m,49.13m,52.71m,56.56m,60.69m,65.13m,69.90m,75.05m,80.59m,86.57m,93.02m };
        decimal[] ssv7_15_f2im = { 0,835.88m,799.98m,761.46m,720.11m,675.72m,628.06m,576.88m,521.90m,462.83m,399.31m,330.97m,257.38m,178.06m,92.46m };
        decimal[] ssv7_15_f2de = { 0,483.79m,520.89m,560.92m,604.12m,650.78m,701.18m,755.69m,714.67m,660.71m,592.79m,509.79m,410.47m,293.45m,157.19m };
        decimal[] ssv7_15_f2tw = { 0,241.67m,260.21m,280.20m,301.78m,225.09m,142.52m,153.60m,165.59m,178.58m,92.65m,0,0,0,0 };
        for (int i = 0; i < ssv7_15_f1.Length; i++)
        {
            if (ssv7_15_f1[i] != 0 || ssv7_15_f2im[i] != 0)
                _db.SsvFactors.Add(new SsvFactor { Id=200+i, Ppt=7, Pt=15, Option="Immediate", PolicyYear=i+1, Factor1=ssv7_15_f1[i], Factor2=ssv7_15_f2im[i] });
            if (ssv7_15_f1[i] != 0 || ssv7_15_f2de[i] != 0)
                _db.SsvFactors.Add(new SsvFactor { Id=220+i, Ppt=7, Pt=15, Option="Deferred",  PolicyYear=i+1, Factor1=ssv7_15_f1[i], Factor2=ssv7_15_f2de[i] });
            if (ssv7_15_f1[i] != 0 || ssv7_15_f2tw[i] != 0)
                _db.SsvFactors.Add(new SsvFactor { Id=240+i, Ppt=7, Pt=15, Option="Twin",      PolicyYear=i+1, Factor1=ssv7_15_f1[i], Factor2=ssv7_15_f2tw[i] });
        }

        // Seed SSV factors for PPT=7, PT=20 (to validate PT-specific selection)
        decimal[] ssv7_20_f1   = { 0,26.74m,28.63m,30.66m,32.84m,35.16m,37.66m,40.33m,43.20m,46.27m,49.56m,53.09m,56.88m,60.95m,65.33m,70.05m,75.14m,80.64m,86.59m,93.02m };
        decimal[] ssv7_20_f2im = { 0,973.29m,947.94m,920.78m,891.70m,860.56m,827.22m,791.52m,753.30m,712.37m,668.52m,621.53m,571.12m,517.00m,458.83m,396.23m,328.76m,255.96m,177.30m,92.19m };
        for (int i = 0; i < ssv7_20_f1.Length; i++)
            if (ssv7_20_f1[i] != 0 || ssv7_20_f2im[i] != 0)
                _db.SsvFactors.Add(new SsvFactor { Id=300+i, Ppt=7, Pt=20, Option="Immediate", PolicyYear=i+1, Factor1=ssv7_20_f1[i], Factor2=ssv7_20_f2im[i] });

        // Seed Loyalty factors for PPT=7
        _db.LoyaltyFactors.AddRange(
            new LoyaltyFactor { Id=300, Ppt=7, PolicyYearFrom=2, PolicyYearTo=2,   RatePercent=3m },
            new LoyaltyFactor { Id=301, Ppt=7, PolicyYearFrom=3, PolicyYearTo=3,   RatePercent=6m },
            new LoyaltyFactor { Id=302, Ppt=7, PolicyYearFrom=4, PolicyYearTo=4,   RatePercent=9m },
            new LoyaltyFactor { Id=303, Ppt=7, PolicyYearFrom=5, PolicyYearTo=5,   RatePercent=12m },
            new LoyaltyFactor { Id=304, Ppt=7, PolicyYearFrom=6, PolicyYearTo=6,   RatePercent=15m },
            new LoyaltyFactor { Id=305, Ppt=7, PolicyYearFrom=7, PolicyYearTo=null, RatePercent=18m }
        );

        // Seed Deferred income factors for PPT=7, PT=15
        _db.DeferredIncomeFactors.AddRange(
            new DeferredIncomeFactor { Id=400, Ppt=7, Pt=15, PolicyYear=8,  RatePercent=30m },
            new DeferredIncomeFactor { Id=401, Ppt=7, Pt=15, PolicyYear=9,  RatePercent=33m },
            new DeferredIncomeFactor { Id=402, Ppt=7, Pt=15, PolicyYear=10, RatePercent=36m },
            new DeferredIncomeFactor { Id=403, Ppt=7, Pt=15, PolicyYear=11, RatePercent=39m },
            new DeferredIncomeFactor { Id=404, Ppt=7, Pt=15, PolicyYear=12, RatePercent=42m },
            new DeferredIncomeFactor { Id=405, Ppt=7, Pt=15, PolicyYear=13, RatePercent=45m },
            new DeferredIncomeFactor { Id=406, Ppt=7, Pt=15, PolicyYear=14, RatePercent=48m },
            new DeferredIncomeFactor { Id=407, Ppt=7, Pt=15, PolicyYear=15, RatePercent=51m }
        );

        _db.SaveChanges();
        _svc = new BenefitCalculationService(_db);
    }

    [OneTimeTearDown]
    public void TearDown() => _db.Dispose();

    private static BenefitIllustrationRequest ImmediateRequest(decimal annualPremium = 50000m, int ppt = 7, int pt = 15, int age = 35, string channel = "Other") =>
        new() { AnnualPremium = annualPremium, Ppt = ppt, PolicyTerm = pt, EntryAge = age, Option = "Immediate", Channel = channel };

    [Test]
    public async Task ImmediateIncome_SAD_Is10xAP()
    {
        var ap = 50000m;
        var result = await _svc.CalculateAsync(ImmediateRequest(ap));
        // SA = 10 × Annual Premium
        var expected = Math.Round(10m * ap, 2, MidpointRounding.AwayFromZero);
        Assert.AreEqual(expected, result.SumAssuredOnDeath);
    }

    [Test]
    public async Task ImmediateIncome_GMB_AppliesHighPremiumAndChannelBenefits()
    {
        // AP=150000 (>= 100k < 200k), Online, Immediate
        // BaseGMB = 150000 × 11.5 = 1725000
        // GMB1 = 1725000 × 1.03 = 1776750
        // FinalGMB = 1776750 × 1.0425 = 1852581.75
        var req = new BenefitIllustrationRequest
        {
            AnnualPremium = 150000m, Ppt = 7, PolicyTerm = 15,
            EntryAge = 35, Option = "Immediate", Channel = "Online"
        };
        var result = await _svc.CalculateAsync(req);
        var expected = Math.Round(150000m * 11.5m * 1.03m * 1.0425m, 2, MidpointRounding.AwayFromZero);
        Assert.AreEqual(expected, result.GuaranteedMaturityBenefit);
    }

    [Test]
    public async Task ImmediateIncome_GI_Is10PercentAP_EachYear()
    {
        var result = await _svc.CalculateAsync(ImmediateRequest(50000m));
        var expectedGI = Math.Round(0.10m * 50000m, 2, MidpointRounding.AwayFromZero);
        foreach (var row in result.YearlyTable)
            Assert.AreEqual(expectedGI, row.GuaranteedIncome, $"PY={row.PolicyYear}");
    }

    [Test]
    public async Task DeferredIncome_GI_StartsAfterPPT()
    {
        var req = new BenefitIllustrationRequest
        {
            AnnualPremium = 50000m, Ppt = 7, PolicyTerm = 15,
            EntryAge = 35, Option = "Deferred", Channel = "Other"
        };
        var result = await _svc.CalculateAsync(req);
        foreach (var row in result.YearlyTable.Where(r => r.PolicyYear <= 7))
            Assert.AreEqual(0m, row.GuaranteedIncome, $"PY={row.PolicyYear} should have GI=0 for deferred");
        foreach (var row in result.YearlyTable.Where(r => r.PolicyYear > 7))
            Assert.Greater(row.GuaranteedIncome, 0m, $"PY={row.PolicyYear} should have GI>0 for deferred");
    }

    [Test]
    public async Task ReducedPaidUp_ReducesAllBenefitsProportionally()
    {
        // t=3, n=7: reduction = 3/7
        var req = new BenefitIllustrationRequest
        {
            AnnualPremium = 50000m, Ppt = 7, PolicyTerm = 15,
            EntryAge = 35, Option = "Immediate", Channel = "Other",
            PremiumsPaid = 3
        };
        var fullReq = ImmediateRequest(50000m);

        var paidUpResult = await _svc.CalculateAsync(req);
        var fullResult = await _svc.CalculateAsync(fullReq);

        var ratio = 3m / 7m;
        var py1PaidUp = paidUpResult.YearlyTable[0];
        var py1Full = fullResult.YearlyTable[0];

        var expectedGI = Math.Round(py1Full.GuaranteedIncome * ratio, 2, MidpointRounding.AwayFromZero);
        Assert.AreEqual(expectedGI, py1PaidUp.GuaranteedIncome);
        Assert.IsTrue(py1PaidUp.IsPaidUp);
    }

    [Test]
    public async Task SurrenderValue_IsMaxOfGsvAndSsv()
    {
        var result = await _svc.CalculateAsync(ImmediateRequest());
        foreach (var row in result.YearlyTable)
        {
            var expected = Math.Max(0m, Math.Max(row.Gsv, row.Ssv));
            Assert.AreEqual(expected, row.SurrenderValue, $"PY={row.PolicyYear}");
        }
    }

    [Test]
    public async Task DeathBenefit_IsMaxOf3Components()
    {
        var ap = 50000m;
        var result = await _svc.CalculateAsync(ImmediateRequest(ap));
        // SAD = Max(10×AP, GMB)
        var sad = result.SumAssuredOnDeath;
        foreach (var row in result.YearlyTable)
        {
            var expected = Math.Round(
                Math.Max(sad, Math.Max(row.SurrenderValue, 1.05m * row.TotalPremiumsPaid)),
                2, MidpointRounding.AwayFromZero);
            Assert.AreEqual(expected, row.DeathBenefit, $"PY={row.PolicyYear}");
        }
    }

    [Test]
    public async Task MaturityBenefit_OnlyInFinalYear()
    {
        var result = await _svc.CalculateAsync(ImmediateRequest());
        var pt = result.PolicyTerm;
        foreach (var row in result.YearlyTable)
        {
            if (row.PolicyYear == pt)
                Assert.Greater(row.MaturityBenefit, 0m, "Final year must have maturity benefit");
            else
                Assert.AreEqual(0m, row.MaturityBenefit, $"PY={row.PolicyYear} should have no maturity benefit");
        }
    }

    [Test]
    public async Task MaxLoan_Is70PercentOfFinalSV()
    {
        var result = await _svc.CalculateAsync(ImmediateRequest());
        var lastSv = result.YearlyTable.Last().SurrenderValue;
        var expected = Math.Round(0.70m * lastSv, 2, MidpointRounding.AwayFromZero);
        Assert.AreEqual(expected, result.MaxLoanAmount);
    }

    [Test]
    public async Task TwinIncome_PaidInSpecificYears()
    {
        // PPT=7, PT=15: firstPairStart=5, secondPairStart=10 => years {5,6,10,11}
        var req = new BenefitIllustrationRequest
        {
            AnnualPremium = 50000m, Ppt = 7, PolicyTerm = 15,
            EntryAge = 35, Option = "Twin", Channel = "Other"
        };
        var result = await _svc.CalculateAsync(req);
        var twinYears = new HashSet<int> { 5, 6, 10, 11 };
        foreach (var row in result.YearlyTable)
        {
            if (twinYears.Contains(row.PolicyYear))
                Assert.Greater(row.GuaranteedIncome, 0m, $"PY={row.PolicyYear} should be a twin income year");
            else
                Assert.AreEqual(0m, row.GuaranteedIncome, $"PY={row.PolicyYear} should not be a twin income year");
        }
    }

    /// <summary>
    /// Validates that when PPT=7 has two PTs (15 and 20), the correct GSV/SSV factor set is
    /// selected based on the requested PT.  PY8 differs: PT=15 → 55.71%, PT=20 → 53.33%.
    /// </summary>
    [Test]
    public async Task GsvFactor_SelectsCorrectPt_WhenSamePptHasTwoPts()
    {
        // PT=15 at PY8 should use GSV 55.71%
        var req15 = ImmediateRequest(ppt: 7, pt: 15);
        var result15 = await _svc.CalculateAsync(req15);
        var py8_15 = result15.YearlyTable.First(r => r.PolicyYear == 8);

        // PT=20 at PY8 should use GSV 53.33%
        var req20 = new BenefitIllustrationRequest
        {
            AnnualPremium = 50000m, Ppt = 7, PolicyTerm = 20,
            EntryAge = 35, Option = "Immediate", Channel = "Other"
        };
        var result20 = await _svc.CalculateAsync(req20);
        var py8_20 = result20.YearlyTable.First(r => r.PolicyYear == 8);

        // GSV = (factorPct/100) × totalPremiums – cumulativeSurvivalBenefits
        // Both have 8 × 50000 = 400000 premiums and same GI/LI, but factors differ.
        // PT=15 factor (55.71%) > PT=20 factor (53.33%), so GSV(PT=15) > GSV(PT=20).
        Assert.Greater(py8_15.Gsv, py8_20.Gsv,
            "PT=15 GSV factor at PY8 (55.71%) should exceed PT=20 GSV factor (53.33%)");
    }

    /// <summary>
    /// Validates that SSV also uses PT-specific factors: at PY2 Factor1 differs between PT=15 and PT=20.
    /// </summary>
    [Test]
    public async Task SsvFactor_SelectsCorrectPt_WhenSamePptHasTwoPts()
    {
        // PT=15: Factor1 at PY2 = 37.13  →  SSV Factor1 part = (37.13/100) × GMB
        // PT=20: Factor1 at PY2 = 26.74  →  SSV Factor1 part = (26.74/100) × GMB
        var req15 = ImmediateRequest(ppt: 7, pt: 15);
        var result15 = await _svc.CalculateAsync(req15);
        var py2_15 = result15.YearlyTable.First(r => r.PolicyYear == 2);

        var req20 = new BenefitIllustrationRequest
        {
            AnnualPremium = 50000m, Ppt = 7, PolicyTerm = 20,
            EntryAge = 35, Option = "Immediate", Channel = "Other"
        };
        var result20 = await _svc.CalculateAsync(req20);
        var py2_20 = result20.YearlyTable.First(r => r.PolicyYear == 2);

        // PT=15 Factor1 (37.13%) > PT=20 Factor1 (26.74%), so SSV(PT=15) > SSV(PT=20).
        Assert.Greater(py2_15.Ssv, py2_20.Ssv,
            "PT=15 SSV Factor1 at PY2 (37.13%) should exceed PT=20 Factor1 (26.74%)");
    }
}
