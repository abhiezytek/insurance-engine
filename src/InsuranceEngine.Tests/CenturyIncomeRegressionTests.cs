using InsuranceEngine.Api;
using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Exceptions;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;

namespace InsuranceEngine.Tests;

/// <summary>
/// Regression tests for SUD Life Century Income covering:
/// A) Option alias acceptance and normalization
/// B) Surrender value source selection (GSV vs SSV)
/// C) Annual premium vs annualized premium basis under non-annual modes
/// D) Option-specific feature validation (Special Date, Premium Offset)
/// </summary>
[TestFixture]
public class CenturyIncomeRegressionTests
{
    private BenefitCalculationService _svc = null!;
    private InsuranceDbContext _db = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase("CenturyIncomeRegression_" + Guid.NewGuid())
            .Options;
        _db = new InsuranceDbContext(options);

        SeedData.SeedAsync(_db).GetAwaiter().GetResult();
        _svc = new BenefitCalculationService(_db);
    }

    [OneTimeTearDown]
    public void TearDown() => _db.Dispose();

    private static BenefitIllustrationRequest Request(
        decimal annualisedPremium, int ppt, int pt, int age, string option,
        string frequency = "Yearly") =>
        new()
        {
            AnnualisedPremium = annualisedPremium,
            Ppt = ppt,
            PolicyTerm = pt,
            EntryAge = age,
            Option = option,
            PremiumFrequency = frequency
        };

    // -----------------------------------------------------------------------
    // A) Option alias acceptance and normalization
    // -----------------------------------------------------------------------

    [TestCase("Immediate")]
    [TestCase("Immediate Income")]
    [TestCase("Deferred")]
    [TestCase("Deferred Income")]
    [TestCase("Twin")]
    [TestCase("Twin Income")]
    public void NormalizeOption_AcceptsAllSixInputs(string input)
    {
        var result = CenturyIncomeFactorLoader.NormalizeOption(input);
        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result);
    }

    [TestCase("Immediate", "Immediate Income", "Immediate")]
    [TestCase("Deferred", "Deferred Income", "Deferred")]
    [TestCase("Twin", "Twin Income", "Twin")]
    public void NormalizeOption_ShortAndLongFormMapToSameCanonical(string shortForm, string longForm, string expected)
    {
        var fromShort = CenturyIncomeFactorLoader.NormalizeOption(shortForm);
        var fromLong = CenturyIncomeFactorLoader.NormalizeOption(longForm);

        Assert.AreEqual(expected, fromShort, $"Short form '{shortForm}' should normalize to '{expected}'");
        Assert.AreEqual(expected, fromLong, $"Long form '{longForm}' should normalize to '{expected}'");
        Assert.AreEqual(fromShort, fromLong, "Short and long forms must normalize to the same value");
    }

    [Test]
    public void NormalizeOption_NullOrEmpty_ThrowsInvalidOperation()
    {
        Assert.Throws<InvalidOperationException>(() => CenturyIncomeFactorLoader.NormalizeOption(null));
        Assert.Throws<InvalidOperationException>(() => CenturyIncomeFactorLoader.NormalizeOption(""));
        Assert.Throws<InvalidOperationException>(() => CenturyIncomeFactorLoader.NormalizeOption("   "));
    }

    [Test]
    public void NormalizeOption_UnsupportedValue_ThrowsInvalidOperation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CenturyIncomeFactorLoader.NormalizeOption("SuperGold"));
        Assert.IsNotNull(ex);
        StringAssert.Contains("Unsupported option", ex!.Message);
    }

    [TestCase("Immediate Income")]
    [TestCase("Deferred Income")]
    [TestCase("Twin Income")]
    public async Task LongFormOption_ProducesSameResultAsShortForm(string longForm)
    {
        var shortForm = CenturyIncomeFactorLoader.NormalizeOption(longForm);
        int ppt = longForm.Contains("Immediate") ? 7 : longForm.Contains("Deferred") ? 10 : 12;
        int pt = longForm.Contains("Immediate") ? 15 : longForm.Contains("Deferred") ? 20 : 25;

        var shortResult = await _svc.CalculateAsync(Request(50000m, ppt, pt, 30, shortForm));
        var longResult = await _svc.CalculateAsync(Request(50000m, ppt, pt, 30, longForm));

        Assert.AreEqual(shortResult.SumAssuredOnDeath, longResult.SumAssuredOnDeath,
            "SAD must be identical for short and long option forms");
        Assert.AreEqual(shortResult.GuaranteedMaturityBenefit, longResult.GuaranteedMaturityBenefit,
            "GMB must be identical for short and long option forms");
        Assert.AreEqual(shortResult.YearlyTable.Count, longResult.YearlyTable.Count,
            "Yearly table row count must match");

        for (int i = 0; i < shortResult.YearlyTable.Count; i++)
        {
            Assert.AreEqual(shortResult.YearlyTable[i].SurrenderValue, longResult.YearlyTable[i].SurrenderValue,
                $"SV mismatch at PY={i + 1}");
            Assert.AreEqual(shortResult.YearlyTable[i].DeathBenefit, longResult.YearlyTable[i].DeathBenefit,
                $"DB mismatch at PY={i + 1}");
        }
    }

    // -----------------------------------------------------------------------
    // B) Surrender value source selection (GSV vs SSV)
    // -----------------------------------------------------------------------

    [Test]
    public async Task SurrenderValue_SsvGreaterThanGsv_SelectsSsv()
    {
        // Immediate 7/15: SSV dominates in all years for this combination
        var result = await _svc.CalculateAsync(Request(50000m, 7, 15, 30, "Immediate"));
        var ssvRow = result.YearlyTable.FirstOrDefault(r => r.Ssv > r.Gsv && r.Ssv > 0);
        Assert.IsNotNull(ssvRow, "Expected at least one year where SSV > GSV for Immediate 7/15");
        Assert.AreEqual(ssvRow!.Ssv, ssvRow.SurrenderValue,
            $"PY={ssvRow.PolicyYear}: SV should equal SSV when SSV > GSV");
        Assert.AreEqual("SSV", ssvRow.SurrenderValueSource,
            $"PY={ssvRow.PolicyYear}: SurrenderValueSource should be SSV");
    }

    [Test]
    public async Task SurrenderValue_GsvGreaterThanSsv_SelectsGsv()
    {
        // Deferred 10/25: GSV dominates in mid-years (PY 9-13) while SSV dominates later
        var result = await _svc.CalculateAsync(Request(50000m, 10, 25, 30, "Deferred"));
        var gsvRow = result.YearlyTable.FirstOrDefault(r => r.Gsv > r.Ssv && r.Gsv > 0);
        Assert.IsNotNull(gsvRow, "Expected at least one year where GSV > SSV for Deferred 10/25");
        Assert.AreEqual(gsvRow!.Gsv, gsvRow.SurrenderValue,
            $"PY={gsvRow.PolicyYear}: SV should equal GSV when GSV > SSV");
        Assert.AreEqual("GSV", gsvRow.SurrenderValueSource,
            $"PY={gsvRow.PolicyYear}: SurrenderValueSource should be GSV");
    }

    [Test]
    public async Task SurrenderValueSource_FlipsCorrectly_AcrossAllYears()
    {
        // Deferred 10/25 has both GSV-dominant and SSV-dominant years, ideal for flip test
        var result = await _svc.CalculateAsync(Request(50000m, 10, 25, 30, "Deferred"));
        foreach (var row in result.YearlyTable)
        {
            var expected = Math.Max(row.Gsv, row.Ssv);
            Assert.AreEqual(expected, row.SurrenderValue,
                $"PY={row.PolicyYear}: SV should be MAX(GSV={row.Gsv}, SSV={row.Ssv})");

            if (row.SurrenderValue > 0)
            {
                var expectedSource = row.Ssv >= row.Gsv ? "SSV" : "GSV";
                Assert.AreEqual(expectedSource, row.SurrenderValueSource,
                    $"PY={row.PolicyYear}: Source should be {expectedSource} (GSV={row.Gsv}, SSV={row.Ssv})");
            }
        }
    }

    [Test]
    public void MissingGsvFactors_ThrowsExpectedError()
    {
        // Use a separate in-memory DB with NO factors seeded
        var opts = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase("EmptyFactorDb_" + Guid.NewGuid())
            .Options;
        using var emptyDb = new InsuranceDbContext(opts);
        var svc = new BenefitCalculationService(emptyDb);

        // GMB lookup fails first with ProductConfigurationException
        var ex = Assert.ThrowsAsync<ProductConfigurationException>(
            () => svc.CalculateAsync(Request(50000m, 7, 15, 30, "Immediate")));
        Assert.IsNotNull(ex);
        Assert.IsTrue(
            ex!.Message.Contains("factor", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase),
            $"Error message should mention missing factor but got: {ex.Message}");
    }

    // -----------------------------------------------------------------------
    // C) Annual premium vs annualized premium basis
    // -----------------------------------------------------------------------

    [Test]
    public async Task MonthlyMode_AnnualPremiumDiffersFromAnnualisedPremium()
    {
        // Monthly modal factor = 0.0867, payments = 12
        // Annualized Premium = 50000, Annual Premium = 50000 * 0.0867 * 12 = 52020
        var result = await _svc.CalculateAsync(Request(50000m, 7, 15, 30, "Immediate", "Monthly"));
        Assert.AreNotEqual(result.AnnualisedPremium, result.AnnualPremium,
            "For monthly mode, Annual Premium must differ from Annualised Premium due to modal factor loading");
        Assert.AreEqual(50000m, result.AnnualisedPremium, "Annualised Premium should echo input");

        // Annual Premium ≈ 50000 * 0.0867 * 12 = 52020
        var expectedAP = Math.Round(50000m * 0.0867m * 12m, 2, MidpointRounding.AwayFromZero);
        Assert.AreEqual(expectedAP, result.AnnualPremium,
            "Annual Premium = AnnualisedPremium × ModalFactor × PaymentsPerYear");
    }

    [Test]
    public async Task MonthlyMode_DeathBenefit_UsesTenTimesAnnualPremium()
    {
        var result = await _svc.CalculateAsync(Request(50000m, 7, 15, 30, "Immediate", "Monthly"));
        var expectedSAD = Math.Round(10m * result.AnnualPremium, 2, MidpointRounding.AwayFromZero);
        Assert.AreEqual(expectedSAD, result.SumAssuredOnDeath,
            "SAD = 10 × Annual Premium (not 10 × Annualised Premium)");
        // Verify it's NOT based on annualised premium
        var incorrectSAD = Math.Round(10m * result.AnnualisedPremium, 2, MidpointRounding.AwayFromZero);
        Assert.AreNotEqual(incorrectSAD, result.SumAssuredOnDeath,
            "SAD should not be 10 × Annualised Premium for non-annual modes");
    }

    [Test]
    public async Task MonthlyMode_GuaranteedIncome_UsesAnnualisedPremium()
    {
        var result = await _svc.CalculateAsync(Request(50000m, 7, 15, 30, "Immediate", "Monthly"));
        // Immediate GI = 10% of Annualised Premium
        var expectedGI = Math.Round(0.10m * 50000m, 2, MidpointRounding.AwayFromZero);
        var firstGI = result.YearlyTable.First().GuaranteedIncome;
        Assert.AreEqual(expectedGI, firstGI,
            "GI should be 10% of Annualised Premium (not Annual Premium)");
        // Verify it's NOT based on annual premium
        var incorrectGI = Math.Round(0.10m * result.AnnualPremium, 2, MidpointRounding.AwayFromZero);
        Assert.AreNotEqual(incorrectGI, firstGI,
            "GI should not be derived from Annual Premium for non-annual modes");
    }

    [TestCase("Half Yearly", 0.5108, 2)]
    [TestCase("Quarterly", 0.2582, 4)]
    [TestCase("Monthly", 0.0867, 12)]
    public async Task NonAnnualMode_ModalFactorAppliesCorrectly(string frequency, double modalFactor, int payments)
    {
        var annualisedPremium = 50000m;
        var result = await _svc.CalculateAsync(Request(annualisedPremium, 7, 15, 30, "Immediate", frequency));

        var expectedAP = Math.Round(annualisedPremium * (decimal)modalFactor * payments, 2, MidpointRounding.AwayFromZero);
        Assert.AreEqual(expectedAP, result.AnnualPremium,
            $"Annual Premium for {frequency}: AP = {annualisedPremium} × {modalFactor} × {payments}");

        var expectedSAD = Math.Round(10m * expectedAP, 2, MidpointRounding.AwayFromZero);
        Assert.AreEqual(expectedSAD, result.SumAssuredOnDeath,
            $"SAD for {frequency} = 10 × Annual Premium");
    }

    [Test]
    public async Task MonthlyMode_MatchesBISample_AnnualisedPremium50000()
    {
        // BI sample: Monthly annualized premium 50,000 → Annual Premium ~52,020
        var result = await _svc.CalculateAsync(Request(50000m, 7, 15, 30, "Immediate", "Monthly"));
        // 50000 × 0.0867 × 12 = 52020.00
        Assert.AreEqual(52020.00m, result.AnnualPremium,
            "Monthly mode: Annualised 50000 should produce Annual Premium ≈ 52020 per BI conventions");
    }

    [Test]
    public async Task MonthlyMode_GmbUsesAnnualisedPremiumBasis()
    {
        var ap = 50000m;
        var resultMonthly = await _svc.CalculateAsync(Request(ap, 7, 15, 30, "Immediate", "Monthly"));
        var resultYearly = await _svc.CalculateAsync(Request(ap, 7, 15, 30, "Immediate", "Yearly"));
        // GMB is linked to Annualised Premium, so it should be the same regardless of frequency
        Assert.AreEqual(resultYearly.GuaranteedMaturityBenefit, resultMonthly.GuaranteedMaturityBenefit,
            "GMB should be identical for Monthly and Yearly when Annualised Premium is the same");
    }

    // -----------------------------------------------------------------------
    // D) Option-specific feature validation
    // -----------------------------------------------------------------------

    // TODO: Special Date option — the request model does not yet expose a
    // SpecialDate / SurvivalDate property. When this field is added,
    // uncomment and verify:
    //   - Immediate Income allows Special Date
    //   - Deferred Income allows Special Date
    //   - Twin Income rejects Special Date with a validation error
    // Missing dependency: BenefitIllustrationRequest.SpecialDate field

    // TODO: Premium Offset — the request model does not yet expose a
    // PremiumOffset property. When this feature is added, add regression
    // tests covering the interaction of Premium Offset with option type
    // and accrued income behavior.
    // Missing dependency: BenefitIllustrationRequest.PremiumOffset field

    [TestCase("Immediate", 7, 15)]
    [TestCase("Deferred", 10, 20)]
    [TestCase("Twin", 12, 25)]
    public async Task AllOptions_ProduceValidCalculation_NoExceptions(string option, int ppt, int pt)
    {
        // Regression guard: all three option types must produce valid calculations
        var result = await _svc.CalculateAsync(Request(50000m, ppt, pt, 30, option));
        Assert.IsNotNull(result);
        Assert.AreEqual(pt, result.YearlyTable.Count);
        Assert.Greater(result.SumAssuredOnDeath, 0m);
    }
}

/// <summary>
/// Integration tests for Century Income option alias acceptance via the API endpoint.
/// </summary>
[TestFixture]
public class CenturyIncomeApiRegressionTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        DocFileHelper.EnsureDocFilesAvailable();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<InsuranceDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    var dbName = "CenturyIncomeApiRegression_" + Guid.NewGuid();
                    services.AddDbContext<InsuranceDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));
                });
            });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InsuranceDbContext>();
        db.Database.EnsureCreated();
        SeedData.SeedAsync(db).GetAwaiter().GetResult();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static BenefitIllustrationRequest ValidRequest(string option = "Immediate", int ppt = 7, int pt = 15) => new()
    {
        AnnualisedPremium = 50000m,
        Ppt = ppt,
        PolicyTerm = pt,
        EntryAge = 30,
        Option = option,
        PremiumFrequency = "Yearly",
    };

    [TestCase("Immediate")]
    [TestCase("Immediate Income")]
    [TestCase("Deferred")]
    [TestCase("Deferred Income")]
    [TestCase("Twin")]
    [TestCase("Twin Income")]
    public async Task AllSixOptionAliases_AcceptedByApi(string option)
    {
        int ppt = option.Contains("Immediate") ? 7 : option.Contains("Deferred") ? 10 : 12;
        int pt = option.Contains("Immediate") ? 15 : option.Contains("Deferred") ? 20 : 25;

        var req = ValidRequest(option, ppt, pt);
        var response = await _client.PostAsJsonAsync("/api/benefit-illustration/calculate", req);
        var body = await response.Content.ReadAsStringAsync();

        Assert.IsFalse(body.Contains("Unsupported option"),
            $"Option '{option}' should be accepted but got: {body}");
    }

    [TestCase("Immediate Income", "Immediate")]
    [TestCase("Deferred Income", "Deferred")]
    [TestCase("Twin Income", "Twin")]
    public async Task LongFormOption_ReturnsSameCalculation_AsShortForm(string longForm, string shortForm)
    {
        int ppt = longForm.Contains("Immediate") ? 7 : longForm.Contains("Deferred") ? 10 : 12;
        int pt = longForm.Contains("Immediate") ? 15 : longForm.Contains("Deferred") ? 20 : 25;

        var shortResponse = await _client.PostAsJsonAsync("/api/benefit-illustration/calculate", ValidRequest(shortForm, ppt, pt));
        var longResponse = await _client.PostAsJsonAsync("/api/benefit-illustration/calculate", ValidRequest(longForm, ppt, pt));

        // Both should succeed (not return "Unsupported option")
        var shortBody = await shortResponse.Content.ReadAsStringAsync();
        var longBody = await longResponse.Content.ReadAsStringAsync();

        Assert.IsFalse(shortBody.Contains("Unsupported option"), $"Short form '{shortForm}' rejected");
        Assert.IsFalse(longBody.Contains("Unsupported option"), $"Long form '{longForm}' rejected");

        // If both return 200, verify identical calculation results
        if (shortResponse.StatusCode == HttpStatusCode.OK && longResponse.StatusCode == HttpStatusCode.OK)
        {
            var shortResult = await shortResponse.Content.ReadFromJsonAsync<BenefitIllustrationResponse>();
            var longResult = await longResponse.Content.ReadFromJsonAsync<BenefitIllustrationResponse>();
            Assert.IsNotNull(shortResult);
            Assert.IsNotNull(longResult);
            Assert.AreEqual(shortResult!.SumAssuredOnDeath, longResult!.SumAssuredOnDeath,
                "SAD must match for short and long form option aliases via API");
            Assert.AreEqual(shortResult.GuaranteedMaturityBenefit, longResult.GuaranteedMaturityBenefit,
                "GMB must match for short and long form option aliases via API");
        }
    }
}
