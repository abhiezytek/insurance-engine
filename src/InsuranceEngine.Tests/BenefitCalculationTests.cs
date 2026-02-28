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

        // Seed GSV factors for PPT=7
        decimal[] gsv7 = { 0,30,35,40,45,50,55,58,61,64,67,70,75,80,90 };
        for (int i = 0; i < gsv7.Length; i++)
            _db.GsvFactors.Add(new GsvFactor { Id=100+i, Ppt=7, PolicyYear=i+1, FactorPercent=gsv7[i] });

        // Seed SSV factors for PPT=7
        decimal[] ssv7f1 = { 0,40,45,50,55,60,65,68,71,74,77,80,84,90,100 };
        decimal[] ssv7f2 = { 0,20,25,30,35,40,45,48,51,54,57,60,64,70,80 };
        for (int i = 0; i < ssv7f1.Length; i++)
            _db.SsvFactors.Add(new SsvFactor { Id=200+i, Ppt=7, PolicyYear=i+1, Factor1=ssv7f1[i], Factor2=ssv7f2[i] });

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

    private static BenefitIllustrationRequest ImmediateRequest(decimal ap = 50000m, int ppt = 7, int pt = 15, int age = 35, string channel = "Other") =>
        new() { AnnualPremium = ap, Ppt = ppt, PolicyTerm = pt, EntryAge = age, Option = "Immediate", Channel = channel };

    [Test]
    public async Task ImmediateIncome_SAD_Is10TimesAP()
    {
        var result = await _svc.CalculateAsync(ImmediateRequest(50000m));
        Assert.AreEqual(500000m, result.SumAssuredOnDeath);
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
        foreach (var row in result.YearlyTable)
        {
            var expected = Math.Round(
                Math.Max(10m * ap, Math.Max(row.SurrenderValue, 1.05m * row.TotalPremiumsPaid)),
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
}
