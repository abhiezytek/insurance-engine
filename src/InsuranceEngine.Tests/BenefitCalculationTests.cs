using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
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

        // Seed full product data (includes Century Income factors from CSV).
        SeedData.SeedAsync(_db).GetAwaiter().GetResult();
        _svc = new BenefitCalculationService(_db);
    }

    [OneTimeTearDown]
    public void TearDown() => _db.Dispose();

    private static BenefitIllustrationRequest Request(decimal annualisedPremium, int ppt, int pt, int age, string option) =>
        new()
        {
            AnnualisedPremium = annualisedPremium,
            Ppt = ppt,
            PolicyTerm = pt,
            EntryAge = age,
            Option = option,
            PremiumFrequency = "Yearly"
        };

    [Test]
    public async Task SumAssuredOnDeath_IsTenTimesAnnualisedPremium()
    {
        var ap = 50000m;
        var result = await _svc.CalculateAsync(Request(ap, 7, 15, 25, "Immediate"));
        Assert.AreEqual(Math.Round(10 * ap, 2, MidpointRounding.AwayFromZero), result.SumAssuredOnDeath);
    }

    [Test]
    public async Task GMB_UsesAgeAndOptionSpecificCsvFactor()
    {
        // docs/century_income_gmb_factors.csv row: age 18, Immediate, 7/15 => 6.3379
        var result = await _svc.CalculateAsync(Request(50000m, 7, 15, 18, "Immediate"));
        var expected = Math.Round(50000m * 6.3379m, 2, MidpointRounding.AwayFromZero);
        Assert.AreEqual(expected, result.GuaranteedMaturityBenefit);
    }

    [Test]
    public async Task GuaranteedIncome_Immediate_IsTenPercentOfAnnualisedPremium()
    {
        var result = await _svc.CalculateAsync(Request(50000m, 7, 15, 30, "Immediate"));
        var expectedGI = Math.Round(0.10m * 50000m, 2, MidpointRounding.AwayFromZero);
        var mismatch = result.YearlyTable.FirstOrDefault(r => r.GuaranteedIncome != expectedGI);
        Assert.IsTrue(mismatch == null, mismatch != null ? $"Mismatch at PY={mismatch.PolicyYear}: GI={mismatch.GuaranteedIncome}" : "All GI rows match expected");
    }

    [Test]
    public async Task DeferredIncome_StartsAfterPpt()
    {
        var result = await _svc.CalculateAsync(Request(50000m, 7, 15, 30, "Deferred"));
        Assert.IsTrue(result.YearlyTable.Where(r => r.PolicyYear <= 7).All(r => r.GuaranteedIncome == 0m));
        Assert.IsTrue(result.YearlyTable.Where(r => r.PolicyYear > 7).All(r => r.GuaranteedIncome > 0m));
    }

    [Test]
    public async Task Gsv_UsesCsvFactorWithoutBlankYears()
    {
        var result = await _svc.CalculateAsync(Request(50000m, 7, 15, 30, "Immediate"));
        Assert.IsTrue(result.YearlyTable.All(r => r.Gsv >= 0m));
        // PY2 for 7/15 has factor 0.35 → premiumsPaid=100000 → 35000 minus income/loyalty
        var py2 = result.YearlyTable.First(r => r.PolicyYear == 2);
        Assert.Greater(py2.Gsv, 0m);
    }

    [TestCase(7, 15, "Immediate")]
    [TestCase(7, 20, "Immediate")]
    [TestCase(10, 20, "Deferred")]
    [TestCase(10, 25, "Deferred")]
    [TestCase(12, 25, "Twin")]
    public async Task AllAllowedPptPtAndOptions_ProduceYearWiseRows(int ppt, int pt, string option)
    {
        var result = await _svc.CalculateAsync(Request(30000m, ppt, pt, 32, option));
        Assert.AreEqual(pt, result.YearlyTable.Count);
        Assert.IsTrue(result.YearlyTable.All(r => r.PolicyYear >= 1 && r.PolicyYear <= pt));
    }
}
