using InsuranceEngine.Api;
using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;

namespace InsuranceEngine.Tests;

/// <summary>
/// Validates that invalid / unsupported product configurations return controlled
/// 400 BadRequest responses instead of 500 Internal Server Error.
/// </summary>
[TestFixture]
public class ValidationTests
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

                    var dbName = "ValidationTestDb_" + Guid.NewGuid();
                    services.AddDbContext<InsuranceDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));
                });
            });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InsuranceDbContext>();
        db.Database.EnsureCreated();
        SeedMinimalData(db);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static void SeedMinimalData(InsuranceDbContext db)
    {
        var insurer = new Insurer { Name = "Test Insurer", Code = "TI" };
        db.Insurers.Add(insurer);
        db.SaveChanges();

        db.Products.Add(new Product { InsurerId = insurer.Id, Name = "ULIP", Code = "EWEALTH-ROYALE", ProductType = "ULIP" });
        db.SaveChanges();

        // Seed mortality rates for ULIP tests
        int id = 1000;
        for (int age = 18; age <= 65; age++)
        {
            decimal rate = age <= 30 ? 1.10m : age <= 40 ? 1.79m : age <= 50 ? 3.55m : 7.65m;
            db.MortalityRates.Add(new MortalityRate { Id = id++, Age = age, Rate = rate, Gender = "Male", EffectiveDate = DateTime.UtcNow });
            db.MortalityRates.Add(new MortalityRate { Id = id++, Age = age, Rate = rate * 0.80m, Gender = "Female", EffectiveDate = DateTime.UtcNow });
        }
        db.SaveChanges();

        // Seed Century Income factors
        SeedData.SeedAsync(db).GetAwaiter().GetResult();
    }

    // -----------------------------------------------------------------------
    // ULIP / e-Wealth Royale validation tests
    // -----------------------------------------------------------------------

    private static UlipCalculationRequest ValidUlipRequest() => new()
    {
        PolicyNumber = "VAL-TEST-" + Guid.NewGuid().ToString("N")[..8],
        CustomerName = "Validation User",
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
        InvestmentStrategy = "Self-Managed Investment Strategy",
        FundAllocations = new List<UlipFundAllocation>
        {
            new() { FundType = "Equity Growth Fund", AllocationPercent = 100 },
        },
    };

    [Test]
    public async Task Ulip_InvalidStrategy_Returns400()
    {
        var req = ValidUlipRequest();
        req.InvestmentStrategy = "System Managed";
        var response = await _client.PostAsJsonAsync("/api/ulip/calculate", req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Test]
    public async Task Ulip_NegativePremium_Returns400()
    {
        var req = ValidUlipRequest();
        req.AnnualizedPremium = -5000;
        var response = await _client.PostAsJsonAsync("/api/ulip/calculate", req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Test]
    public async Task Ulip_UnsupportedOption_Returns400()
    {
        var req = ValidUlipRequest();
        req.Option = "Diamond";
        var response = await _client.PostAsJsonAsync("/api/ulip/calculate", req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Test]
    public async Task Ulip_UnsupportedFrequency_Returns400()
    {
        var req = ValidUlipRequest();
        req.PremiumFrequency = "Biweekly";
        var response = await _client.PostAsJsonAsync("/api/ulip/calculate", req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Test]
    public async Task Ulip_MissingFundAllocationForSelfManaged_Returns400()
    {
        var req = ValidUlipRequest();
        req.InvestmentStrategy = "Self-Managed Investment Strategy";
        req.FundAllocations = new List<UlipFundAllocation>();
        var response = await _client.PostAsJsonAsync("/api/ulip/calculate", req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Test]
    public async Task Ulip_AllocationNotSumTo100_Returns400()
    {
        var req = ValidUlipRequest();
        req.FundAllocations = new List<UlipFundAllocation>
        {
            new() { FundType = "Equity Growth Fund", AllocationPercent = 50 },
            new() { FundType = "Debt Fund", AllocationPercent = 30 },
        };
        var response = await _client.PostAsJsonAsync("/api/ulip/calculate", req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Test]
    public async Task Ulip_PolicyTermOutOfRange_Returns400()
    {
        var req = ValidUlipRequest();
        req.PolicyTerm = 100;
        req.Ppt = 10;
        var response = await _client.PostAsJsonAsync("/api/ulip/calculate", req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Century Income validation tests
    // -----------------------------------------------------------------------

    private static BenefitIllustrationRequest ValidCenturyIncomeRequest() => new()
    {
        AnnualisedPremium = 50000m,
        Ppt = 7,
        PolicyTerm = 15,
        EntryAge = 30,
        Option = "Immediate",
        PremiumFrequency = "Yearly",
    };

    [Test]
    public async Task CenturyIncome_ZeroPremium_Returns400()
    {
        var req = ValidCenturyIncomeRequest();
        req.AnnualisedPremium = 0;
        req.AnnualPremium = 0;
        var response = await _client.PostAsJsonAsync("/api/benefit-illustration/calculate", req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Test]
    public async Task CenturyIncome_InvalidOption_Returns400()
    {
        var req = ValidCenturyIncomeRequest();
        req.Option = "SuperGold";
        var response = await _client.PostAsJsonAsync("/api/benefit-illustration/calculate", req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Test]
    public async Task CenturyIncome_InvalidFrequency_Returns400()
    {
        var req = ValidCenturyIncomeRequest();
        req.PremiumFrequency = "Biweekly";
        var response = await _client.PostAsJsonAsync("/api/benefit-illustration/calculate", req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Test]
    public async Task CenturyIncome_PptExceedsPt_Returns400()
    {
        var req = ValidCenturyIncomeRequest();
        req.Ppt = 20;
        req.PolicyTerm = 10;
        var response = await _client.PostAsJsonAsync("/api/benefit-illustration/calculate", req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Test]
    public async Task CenturyIncome_ValidRequest_DoesNotReturn500()
    {
        var req = ValidCenturyIncomeRequest();
        var response = await _client.PostAsJsonAsync("/api/benefit-illustration/calculate", req);
        // Should never return 500: either 200 (factors found) or 400 (factors missing)
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Valid request must not produce a 500 Internal Server Error");
    }

    [TestCase("Immediate Income")]
    [TestCase("Deferred Income")]
    [TestCase("Twin Income")]
    [Test]
    public async Task CenturyIncome_FullNameOptions_DoNotReturn400Validation(string optionFullName)
    {
        var req = ValidCenturyIncomeRequest();
        req.Option = optionFullName;
        var response = await _client.PostAsJsonAsync("/api/benefit-illustration/calculate", req);
        // Full product-file names like "Immediate Income" must pass controller validation.
        // The response may be 200 (if factors are seeded) or 400 with a factor-missing
        // message, but never a 400 from option validation itself.
        var body = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(body.Contains("Unsupported option"),
            $"Option '{optionFullName}' should be accepted by validation but got: {body}");
    }
}
