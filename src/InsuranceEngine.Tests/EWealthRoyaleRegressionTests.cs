using InsuranceEngine.Api;
using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace InsuranceEngine.Tests;

/// <summary>
/// Regression tests for SUD Life e-Wealth Royale covering:
/// E) FMC lookup for all 12 funds (data integrity + normalization)
/// F) Investment strategy validation (Self-Managed, Age-based, risk preference rules)
/// </summary>
[TestFixture]
public class EWealthRoyaleFmcRegressionTests
{
    // Expected FMC values from the BI / product documentation
    private static readonly (string FundName, decimal FmcPa, decimal FmcPmApprox)[] ExpectedFunds =
    {
        ("Blue-chip Equity Fund",                  0.0135m, 0.001118m),
        ("Growth Plus Fund",                       0.0135m, 0.001118m),
        ("Balance Plus Fund",                      0.0130m, 0.001077m),
        ("Mid Cap Fund",                           0.0135m, 0.001118m),
        ("Dynamic Fund",                           0.0135m, 0.001118m),
        ("Money Market Fund",                      0.0100m, 0.000830m),
        ("Gilt Fund",                              0.0130m, 0.001077m),
        ("Income Fund",                            0.0130m, 0.001077m),
        ("New India Leaders Fund",                 0.0135m, 0.001118m),
        ("Viksit Bharat Fund",                     0.0135m, 0.001118m),
        ("SUD Life Midcap Momentum Index Fund",    0.0130m, 0.001077m),
        ("SUD Life Nifty Alpha 50 Index Fund",     0.0135m, 0.001118m),
    };

    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static string NormalizeWhitespace(string value) =>
        WhitespaceRegex.Replace(value.Trim(), " ");

    /// <summary>Locate the ewealth_fmc_factors.csv from the docs directory.</summary>
    private static string? FindFmcCsv()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "docs", "ewealth_fmc_factors.csv");
            if (File.Exists(candidate)) return candidate;
            var direct = Path.Combine(current.FullName, "ewealth_fmc_factors.csv");
            if (File.Exists(direct)) return direct;
            current = current.Parent;
        }
        return null;
    }

    /// <summary>Load the CSV into a dictionary keyed by normalized fund name.</summary>
    private static Dictionary<string, (decimal FmcPa, decimal FmcPm)> LoadFmcCsv()
    {
        var path = FindFmcCsv();
        Assert.IsNotNull(path, "ewealth_fmc_factors.csv must exist in the docs directory");

        var dict = new Dictionary<string, (decimal, decimal)>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path!).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',', StringSplitOptions.TrimEntries);
            if (cols.Length < 3) continue;
            var fundName = NormalizeWhitespace(cols[0]);
            var fmcPa = decimal.Parse(cols[1], NumberStyles.Any, CultureInfo.InvariantCulture);
            var fmcPm = decimal.Parse(cols[2], NumberStyles.Any, CultureInfo.InvariantCulture);
            dict[fundName] = (fmcPa, fmcPm);
        }
        return dict;
    }

    // -----------------------------------------------------------------------
    // E.1) Parameterized FMC lookup for all 12 funds
    // -----------------------------------------------------------------------

    [TestCase("Blue-chip Equity Fund",               0.0135, 0.001118)]
    [TestCase("Growth Plus Fund",                    0.0135, 0.001118)]
    [TestCase("Balance Plus Fund",                   0.0130, 0.001077)]
    [TestCase("Mid Cap Fund",                        0.0135, 0.001118)]
    [TestCase("Dynamic Fund",                        0.0135, 0.001118)]
    [TestCase("Money Market Fund",                   0.0100, 0.000830)]
    [TestCase("Gilt Fund",                           0.0130, 0.001077)]
    [TestCase("Income Fund",                         0.0130, 0.001077)]
    [TestCase("New India Leaders Fund",              0.0135, 0.001118)]
    [TestCase("Viksit Bharat Fund",                  0.0135, 0.001118)]
    [TestCase("SUD Life Midcap Momentum Index Fund", 0.0130, 0.001077)]
    [TestCase("SUD Life Nifty Alpha 50 Index Fund",  0.0135, 0.001118)]
    public void FmcCsv_FundResolvesToExpectedRate(string fundName, double expectedFmcPa, double expectedFmcPmApprox)
    {
        var csv = LoadFmcCsv();
        Assert.IsTrue(csv.ContainsKey(fundName),
            $"Fund '{fundName}' not found in FMC CSV (after whitespace normalization). Available: {string.Join(", ", csv.Keys)}");

        var (fmcPa, fmcPm) = csv[fundName];
        Assert.AreEqual((decimal)expectedFmcPa, fmcPa,
            $"FMC p.a. mismatch for '{fundName}'");
        // Monthly FMC is a precise calculation; use tolerance of 0.0001
        Assert.AreEqual((double)fmcPm, (double)expectedFmcPmApprox, 0.0001,
            $"FMC monthly mismatch for '{fundName}'");
    }

    [Test]
    public void FmcCsv_ContainsExactly12Funds()
    {
        var csv = LoadFmcCsv();
        Assert.AreEqual(12, csv.Count, "FMC CSV should contain exactly 12 funds");
    }

    // -----------------------------------------------------------------------
    // E.3) Regression for "SUD Life Nifty Alpha 50 Index Fund"
    // -----------------------------------------------------------------------

    [Test]
    public void NiftyAlpha50_ExactMatchWorks_NoDefaultFallback()
    {
        var csv = LoadFmcCsv();
        const string canonicalName = "SUD Life Nifty Alpha 50 Index Fund";

        Assert.IsTrue(csv.ContainsKey(canonicalName),
            $"'{canonicalName}' must resolve after whitespace normalization. " +
            "The CSV may contain double spaces but NormalizeWhitespace should collapse them.");

        var (fmcPa, fmcPm) = csv[canonicalName];
        // Must be 1.35% and NOT the default FMC
        Assert.AreEqual(0.0135m, fmcPa,
            "Nifty Alpha 50 FMC p.a. should be 1.35%, not a default fallback");
        Assert.Greater(fmcPm, 0m, "Nifty Alpha 50 monthly FMC should be positive");
    }

    [Test]
    public void NiftyAlpha50_CsvHasDoubleSpace_NormalizationFixes()
    {
        // The raw CSV has "SUD Life Nifty  Alpha 50 Index Fund" (double space)
        // After NormalizeWhitespace it should become "SUD Life Nifty Alpha 50 Index Fund"
        const string rawWithDoubleSpace = "SUD Life Nifty  Alpha 50 Index Fund";
        const string canonical = "SUD Life Nifty Alpha 50 Index Fund";

        var normalized = NormalizeWhitespace(rawWithDoubleSpace);
        Assert.AreEqual(canonical, normalized,
            "NormalizeWhitespace should collapse double spaces in fund names");
    }

    // -----------------------------------------------------------------------
    // E.4) NormalizeWhitespace helper behavior
    // -----------------------------------------------------------------------

    [TestCase("  Blue-chip Equity Fund  ", "Blue-chip Equity Fund")]
    [TestCase("Growth  Plus  Fund", "Growth Plus Fund")]
    [TestCase("SUD Life Nifty  Alpha 50 Index Fund", "SUD Life Nifty Alpha 50 Index Fund")]
    [TestCase("Money Market Fund", "Money Market Fund")]
    public void NormalizeWhitespace_CollapsesAndTrims(string input, string expected)
    {
        Assert.AreEqual(expected, NormalizeWhitespace(input));
    }
}

/// <summary>
/// Regression tests for e-Wealth Royale investment strategy validation via the service layer.
/// </summary>
[TestFixture]
public class EWealthRoyaleStrategyRegressionTests
{
    private UlipCalculationService _svc = null!;
    private InsuranceDbContext _db = null!;

    private static UlipCalculationRequest DefaultRequest(
        string policyNumber = "STRAT-REG-001",
        string strategy = "Self-Managed Investment Strategy",
        string? riskPreference = null) =>
        new()
        {
            PolicyNumber = policyNumber,
            CustomerName = "Regression User",
            ProductCode = "EWEALTH-ROYALE",
            Gender = "Male",
            DateOfBirth = DateTime.Today.AddYears(-35),
            EntryAge = 35,
            PolicyTerm = 10,
            Ppt = 10,
            TypeOfPpt = "Regular",
            AnnualizedPremium = 100_000m,
            SumAssured = 1_000_000m,
            PremiumFrequency = "Yearly",
            InvestmentStrategy = strategy,
            RiskPreference = riskPreference,
            FundAllocations = new List<UlipFundAllocation>
            {
                new() { FundType = "Blue-chip Equity Fund", AllocationPercent = 100 },
            },
        };

    [OneTimeSetUp]
    public void SetUp()
    {
        DocFileHelper.EnsureDocFilesAvailable();

        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase("EWealthStrategyRegression_" + Guid.NewGuid())
            .Options;
        _db = new InsuranceDbContext(options);

        var insurer = new Insurer { Id = 1, Name = "Test Insurer", Code = "TI" };
        _db.Insurers.Add(insurer);

        _db.Products.Add(new Product { Id = 1, InsurerId = 1, Name = "ULIP", Code = "EWEALTH-ROYALE", ProductType = "ULIP" });

        _db.UlipCharges.AddRange(
            new UlipCharge { Id = 1, ProductId = 1, ChargeType = "PremiumAllocation", ChargeValue = 0m, ChargeFrequency = "PercentOfPremium" },
            new UlipCharge { Id = 2, ProductId = 1, ChargeType = "FMC", ChargeValue = 1.35m, ChargeFrequency = "PercentOfFund" },
            new UlipCharge { Id = 3, ProductId = 1, ChargeType = "PolicyAdmin", ChargeValue = 100m, ChargeFrequency = "Monthly" }
        );

        int id = 100;
        for (int age = 18; age <= 65; age++)
        {
            decimal rate = age switch
            {
                <= 30 => 1.10m,
                <= 40 => 1.79m,
                <= 50 => 3.55m,
                <= 60 => 7.65m,
                _ => 11.25m,
            };
            _db.MortalityRates.Add(new MortalityRate { Id = id++, Age = age, Rate = rate, Gender = "Male", EffectiveDate = DateTime.UtcNow });
            _db.MortalityRates.Add(new MortalityRate { Id = id++, Age = age, Rate = rate * 0.80m, Gender = "Female", EffectiveDate = DateTime.UtcNow });
        }

        _db.SaveChanges();
        _svc = new UlipCalculationService(_db);
    }

    [OneTimeTearDown]
    public void TearDown() => _db.Dispose();

    // -----------------------------------------------------------------------
    // F.1) Valid strategy acceptance
    // -----------------------------------------------------------------------

    [Test]
    public async Task SelfManagedStrategy_Accepted()
    {
        var req = DefaultRequest(policyNumber: "STRAT-SM-" + Guid.NewGuid().ToString("N")[..6],
            strategy: "Self-Managed Investment Strategy");
        var result = await _svc.CalculateAsync(req);
        Assert.IsNotNull(result);
        Assert.AreEqual(10, result.YearlyTable.Count);
    }

    // -----------------------------------------------------------------------
    // F.2) Invalid strategy rejection
    // -----------------------------------------------------------------------

    [Test]
    public void InvalidStrategy_SystemManaged_ThrowsOrRejects()
    {
        var req = DefaultRequest(policyNumber: "STRAT-INV-" + Guid.NewGuid().ToString("N")[..6],
            strategy: "System Managed");
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _svc.CalculateAsync(req));
        Assert.IsNotNull(ex);
        StringAssert.Contains("not supported", ex!.Message);
    }

    // -----------------------------------------------------------------------
    // F.3) Age-based strategy without risk preference fails
    // -----------------------------------------------------------------------

    [Test]
    public void AgeBasedStrategy_WithoutRiskPreference_Fails()
    {
        var req = DefaultRequest(
            policyNumber: "STRAT-AGE-NRP-" + Guid.NewGuid().ToString("N")[..6],
            strategy: "Age-based Investment Strategy",
            riskPreference: null);
        req.FundAllocations.Clear();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _svc.CalculateAsync(req));
        Assert.IsNotNull(ex);
        StringAssert.Contains("RiskPreference", ex!.Message);
    }

    // -----------------------------------------------------------------------
    // F.4) Self-managed strategy does not require risk preference
    // -----------------------------------------------------------------------

    [Test]
    public async Task SelfManagedStrategy_NoRiskPreference_Succeeds()
    {
        var req = DefaultRequest(
            policyNumber: "STRAT-SM-NRP-" + Guid.NewGuid().ToString("N")[..6],
            strategy: "Self-Managed Investment Strategy",
            riskPreference: null);
        var result = await _svc.CalculateAsync(req);
        Assert.IsNotNull(result);
        Assert.AreEqual(10, result.YearlyTable.Count);
    }

    // -----------------------------------------------------------------------
    // F.5) Self-managed with risk preference (present but ignored)
    // -----------------------------------------------------------------------

    [Test]
    public async Task SelfManagedStrategy_WithRiskPreference_IgnoredAndSucceeds()
    {
        var reqWithPref = DefaultRequest(
            policyNumber: "STRAT-SM-RP-" + Guid.NewGuid().ToString("N")[..6],
            strategy: "Self-Managed Investment Strategy",
            riskPreference: "Aggressive");
        var reqWithout = DefaultRequest(
            policyNumber: "STRAT-SM-NRP2-" + Guid.NewGuid().ToString("N")[..6],
            strategy: "Self-Managed Investment Strategy",
            riskPreference: null);

        var resultWith = await _svc.CalculateAsync(reqWithPref);
        var resultWithout = await _svc.CalculateAsync(reqWithout);

        Assert.IsNotNull(resultWith);
        Assert.IsNotNull(resultWithout);

        // Fund values should be identical regardless of risk preference for Self-Managed
        for (int i = 0; i < resultWith.YearlyTable.Count; i++)
        {
            Assert.AreEqual(resultWithout.YearlyTable[i].FundValue4, resultWith.YearlyTable[i].FundValue4,
                $"Year {i + 1}: FV4 should be identical when risk preference is ignored for Self-Managed");
        }
    }

    // -----------------------------------------------------------------------
    // F.6) Fund allocation must be multiples of 5%
    // -----------------------------------------------------------------------

    [Test]
    public void SelfManaged_AllocationNotMultipleOf5_ThrowsError()
    {
        var req = DefaultRequest(policyNumber: "STRAT-5PCT-" + Guid.NewGuid().ToString("N")[..6]);
        req.FundAllocations = new List<UlipFundAllocation>
        {
            new() { FundType = "Blue-chip Equity Fund", AllocationPercent = 88 },
            new() { FundType = "Gilt Fund", AllocationPercent = 12 },
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _svc.CalculateAsync(req));
        Assert.IsNotNull(ex);
        StringAssert.Contains("multiples of 5%", ex!.Message);
    }
}

/// <summary>
/// Integration tests for e-Wealth Royale strategy validation via the API endpoint.
/// </summary>
[TestFixture]
public class EWealthRoyaleApiRegressionTests
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

                    var dbName = "EWealthApiRegression_" + Guid.NewGuid();
                    services.AddDbContext<InsuranceDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));
                });
            });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InsuranceDbContext>();
        db.Database.EnsureCreated();

        var insurer = new Insurer { Name = "Test Insurer", Code = "TI" };
        db.Insurers.Add(insurer);
        db.SaveChanges();

        db.Products.Add(new Product { InsurerId = insurer.Id, Name = "ULIP", Code = "EWEALTH-ROYALE", ProductType = "ULIP" });
        db.SaveChanges();

        int id = 1000;
        for (int age = 18; age <= 65; age++)
        {
            decimal rate = age <= 30 ? 1.10m : age <= 40 ? 1.79m : age <= 50 ? 3.55m : 7.65m;
            db.MortalityRates.Add(new MortalityRate { Id = id++, Age = age, Rate = rate, Gender = "Male", EffectiveDate = DateTime.UtcNow });
            db.MortalityRates.Add(new MortalityRate { Id = id++, Age = age, Rate = rate * 0.80m, Gender = "Female", EffectiveDate = DateTime.UtcNow });
        }
        db.SaveChanges();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static UlipCalculationRequest ValidUlipRequest(
        string strategy = "Self-Managed Investment Strategy",
        string? riskPreference = null) => new()
    {
        PolicyNumber = "API-REG-" + Guid.NewGuid().ToString("N")[..8],
        CustomerName = "API Regression User",
        ProductCode = "EWEALTH-ROYALE",
        Gender = "Male",
        DateOfBirth = DateTime.Today.AddYears(-35),
        EntryAge = 35,
        PolicyTerm = 10,
        Ppt = 10,
        TypeOfPpt = "Regular",
        AnnualizedPremium = 100_000m,
        SumAssured = 1_000_000m,
        PremiumFrequency = "Yearly",
        InvestmentStrategy = strategy,
        RiskPreference = riskPreference,
        FundAllocations = new List<UlipFundAllocation>
        {
            new() { FundType = "Blue-chip Equity Fund", AllocationPercent = 100 },
        },
    };

    [TestCase("Self-Managed Investment Strategy")]
    [TestCase("Age-based Investment Strategy")]
    public async Task ValidStrategies_DoNotReturn400ForStrategyReason(string strategy)
    {
        var req = ValidUlipRequest(strategy: strategy);
        if (strategy.Contains("Age-based"))
        {
            req.RiskPreference = "Aggressive";
            req.FundAllocations.Clear();
        }

        var response = await _client.PostAsJsonAsync("/api/ulip/calculate", req);
        var body = await response.Content.ReadAsStringAsync();

        Assert.IsFalse(body.Contains("not supported", StringComparison.OrdinalIgnoreCase),
            $"Strategy '{strategy}' should be accepted but got: {body}");
    }

    [Test]
    public async Task InvalidStrategy_Returns400()
    {
        var req = ValidUlipRequest(strategy: "System Managed");
        var response = await _client.PostAsJsonAsync("/api/ulip/calculate", req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Test]
    public async Task AgeBasedStrategy_WithoutRiskPreference_Returns400()
    {
        var req = ValidUlipRequest(strategy: "Age-based Investment Strategy", riskPreference: null);
        req.FundAllocations.Clear();
        var response = await _client.PostAsJsonAsync("/api/ulip/calculate", req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        StringAssert.Contains("RiskPreference", body);
    }

    [Test]
    public async Task SelfManagedStrategy_WithoutRiskPreference_DoesNotReturnStrategyError()
    {
        var req = ValidUlipRequest(strategy: "Self-Managed Investment Strategy", riskPreference: null);
        var response = await _client.PostAsJsonAsync("/api/ulip/calculate", req);
        var body = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(body.Contains("RiskPreference is required", StringComparison.OrdinalIgnoreCase),
            $"Self-managed should not require risk preference, got: {body}");
    }
}
