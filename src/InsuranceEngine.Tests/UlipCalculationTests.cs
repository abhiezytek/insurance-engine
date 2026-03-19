using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InsuranceEngine.Tests;

[TestFixture]
public class UlipCalculationTests
{
    private UlipCalculationService _svc = null!;
    private InsuranceDbContext _db = null!;

    // Default test request
    private static UlipCalculationRequest DefaultRequest(
        string policyNumber  = "TEST-001",
        int    entryAge      = 35,
        int    pt            = 10,
        int    ppt           = 10,
        decimal ap           = 100_000m,
        decimal sa           = 1_000_000m,
        string  gender       = "Male") =>
        new()
        {
            PolicyNumber      = policyNumber,
            CustomerName      = "Test User",
            ProductCode       = "EWEALTH-ROYALE",
            Gender            = gender,
            DateOfBirth       = DateTime.Today.AddYears(-entryAge),
            EntryAge          = entryAge,
            PolicyTerm        = pt,
            Ppt               = ppt,
            TypeOfPpt         = "Regular",
            AnnualizedPremium = ap,
            SumAssured        = sa,
            PremiumFrequency  = "Yearly",
            FundAllocations   = new List<UlipFundAllocation>
            {
                new() { FundType = "Equity Growth Fund", AllocationPercent = 100 },
            },
        };

    [OneTimeSetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase("UlipCalcTestDb_" + Guid.NewGuid())
            .Options;
        _db = new InsuranceDbContext(options);

        // Seed ULIP product + charges + mortality rates
        var insurer = new Insurer { Id = 1, Name = "Test Insurer", Code = "TI" };
        _db.Insurers.Add(insurer);

        var product = new Product { Id = 1, InsurerId = 1, Name = "ULIP", Code = "EWEALTH-ROYALE", ProductType = "ULIP" };
        _db.Products.Add(product);

        _db.UlipCharges.AddRange(
            new UlipCharge { Id = 1, ProductId = 1, ChargeType = "PremiumAllocation", ChargeValue = 0m,    ChargeFrequency = "PercentOfPremium" },
            new UlipCharge { Id = 2, ProductId = 1, ChargeType = "FMC",               ChargeValue = 1.35m, ChargeFrequency = "PercentOfFund"    },
            new UlipCharge { Id = 3, ProductId = 1, ChargeType = "PolicyAdmin",        ChargeValue = 100m,  ChargeFrequency = "Monthly"          }
        );

        // Seed simplified mortality table
        int id = 100;
        for (int age = 18; age <= 65; age++)
        {
            decimal rate = age switch
            {
                <= 30 => 1.10m,
                <= 40 => 1.79m,
                <= 50 => 3.55m,
                <= 60 => 7.65m,
                _     => 11.25m,
            };
            _db.MortalityRates.Add(new MortalityRate { Id = id++, Age = age, Rate = rate, Gender = "Male",   EffectiveDate = DateTime.UtcNow });
            _db.MortalityRates.Add(new MortalityRate { Id = id++, Age = age, Rate = rate * 0.80m, Gender = "Female", EffectiveDate = DateTime.UtcNow });
        }

        _db.SaveChanges();
        _svc = new UlipCalculationService(_db);
    }

    [OneTimeTearDown]
    public void TearDown() => _db.Dispose();

    // -----------------------------------------------------------------------
    // Basic structure tests
    // -----------------------------------------------------------------------

    [Test]
    public async Task Calculate_ReturnsCorrectNumberOfRows()
    {
        var result = await _svc.CalculateAsync(DefaultRequest(pt: 10));
        Assert.AreEqual(10, result.YearlyTable.Count);
    }

    [Test]
    public async Task Calculate_PolicyYearsAreSequential()
    {
        var result = await _svc.CalculateAsync(DefaultRequest(pt: 10));
        for (int i = 0; i < result.YearlyTable.Count; i++)
            Assert.AreEqual(i + 1, result.YearlyTable[i].Year);
    }

    [Test]
    public async Task Calculate_AgeIncrementsEachYear()
    {
        var req = DefaultRequest(entryAge: 35, pt: 10);
        var result = await _svc.CalculateAsync(req);
        for (int i = 0; i < result.YearlyTable.Count; i++)
            Assert.AreEqual(35 + i, result.YearlyTable[i].Age, $"Year {i + 1}");
    }

    // -----------------------------------------------------------------------
    // Premium and charge tests
    // -----------------------------------------------------------------------

    [Test]
    public async Task Calculate_AnnualPremiumZeroAfterPPT()
    {
        // PPT=5, PT=10: years 6..10 should have AnnualPremium=0
        var req = DefaultRequest(pt: 10, ppt: 5);
        req.TypeOfPpt = "Limited";
        var result = await _svc.CalculateAsync(req);
        foreach (var row in result.YearlyTable.Where(r => r.Year > 5))
            Assert.AreEqual(0m, row.AnnualPremium, $"Year {row.Year} should have no premium after PPT");
    }

    [Test]
    public async Task Calculate_PremiumInvested_IsFullAPWithZeroPAC()
    {
        // PAC = 0% → PremiumInvested = AP × 1.0 (full premium invested)
        var req = DefaultRequest(ap: 100_000m, pt: 10, ppt: 1);
        req.TypeOfPpt = "Single";
        var result = await _svc.CalculateAsync(req);
        var expected = Math.Round(100_000m * 1.0m, 2, MidpointRounding.AwayFromZero);
        Assert.AreEqual(expected, result.YearlyTable[0].PremiumInvested);
    }

    [Test]
    public async Task Calculate_PolicyChargeIs1200PerYear_First10Years()
    {
        var result = await _svc.CalculateAsync(DefaultRequest());
        // Policy Admin ₹100/month = ₹1200/year for years 1-10 only
        foreach (var row in result.YearlyTable)
        {
            if (row.Year <= 10)
                Assert.AreEqual(1200m, row.PolicyCharge, $"Year {row.Year} should be ₹1200");
            else
                Assert.AreEqual(0m, row.PolicyCharge, $"Year {row.Year} should be ₹0 (no admin charge after yr 10)");
        }
    }

    [Test]
    public async Task Calculate_MortalityChargeIsPositive()
    {
        var result = await _svc.CalculateAsync(DefaultRequest());
        // All years should have positive mortality charges when SA > 0
        foreach (var row in result.YearlyTable)
            Assert.GreaterOrEqual(row.MortalityCharge, 0m, $"Year {row.Year}");
    }

    // -----------------------------------------------------------------------
    // Fund value growth tests
    // -----------------------------------------------------------------------

    [Test]
    public async Task Calculate_FundValue8_GreaterThanFundValue4()
    {
        var result = await _svc.CalculateAsync(DefaultRequest());
        foreach (var row in result.YearlyTable)
            Assert.GreaterOrEqual(row.FundValue8, row.FundValue4, $"Year {row.Year}: FV8 should be ≥ FV4");
    }

    [Test]
    public async Task Calculate_FundValueGrowsOverTime_4Pct()
    {
        var result = await _svc.CalculateAsync(DefaultRequest(pt: 10, ppt: 10));
        // During premium-paying years (1..10), FV should generally grow
        Assert.Greater(result.YearlyTable[9].FundValue4, result.YearlyTable[0].FundValue4,
            "FV4 at year 10 should exceed FV4 at year 1");
    }

    [Test]
    public async Task Calculate_FundValueGrowsOverTime_8Pct()
    {
        var result = await _svc.CalculateAsync(DefaultRequest(pt: 10, ppt: 10));
        Assert.Greater(result.YearlyTable[9].FundValue8, result.YearlyTable[0].FundValue8,
            "FV8 at year 10 should exceed FV8 at year 1");
    }

    [Test]
    public async Task SumAssured_IsDerived_ForSinglePay()
    {
        var req = DefaultRequest(ap: 100_000m, pt: 10, ppt: 1);
        req.TypeOfPpt = "Single";
        req.SumAssured = 0m;
        var result = await _svc.CalculateAsync(req);
        Assert.AreEqual(Math.Round(1.25m * 100_000m, 2, MidpointRounding.AwayFromZero), result.SumAssured);
    }

    [Test]
    public async Task SumAssured_IsDerived_ForRegularPay()
    {
        var req = DefaultRequest(ap: 50_000m, pt: 10, ppt: 10);
        req.SumAssured = 0m;
        var result = await _svc.CalculateAsync(req);
        Assert.AreEqual(Math.Round(10m * 50_000m, 2, MidpointRounding.AwayFromZero), result.SumAssured);
    }

    // -----------------------------------------------------------------------
    // Death benefit tests
    // -----------------------------------------------------------------------

    [Test]
    public async Task Calculate_DeathBenefit4_IsMaxOfSaFV4And105PctPremiums()
    {
        var req = DefaultRequest(sa: 1_000_000m);
        var result = await _svc.CalculateAsync(req);
        decimal cumPremiums = 0m;
        int ppt = req.Ppt;
        for (int i = 0; i < result.YearlyTable.Count; i++)
        {
            var row = result.YearlyTable[i];
            if (i < ppt) cumPremiums += req.AnnualizedPremium;
            var minDeath = Math.Round(1.05m * cumPremiums, 2, MidpointRounding.AwayFromZero);
            var expected = Math.Round(Math.Max(req.SumAssured, Math.Max(row.FundValue4, minDeath)), 2, MidpointRounding.AwayFromZero);
            Assert.AreEqual(expected, row.DeathBenefit4, $"Year {row.Year}: DB4 = max(SA, FV4, 105%×premiums)");
        }
    }

    [Test]
    public async Task Calculate_DeathBenefit8_IsMaxOfSaAndFV8()
    {
        var req = DefaultRequest(sa: 1_000_000m);
        var result = await _svc.CalculateAsync(req);
        foreach (var row in result.YearlyTable)
        {
            var expected = Math.Round(Math.Max(req.SumAssured, row.FundValue8), 2, MidpointRounding.AwayFromZero);
            Assert.AreEqual(expected, row.DeathBenefit8, $"Year {row.Year}: DB8 = max(SA, FV8)");
        }
    }

    [Test]
    public async Task Calculate_DeathBenefitAtLeastSA_EarlyYears()
    {
        // In early years, FV < SA, so DB should equal SA
        var req = DefaultRequest(sa: 10_000_000m, ap: 100_000m, pt: 10);
        var result = await _svc.CalculateAsync(req);
        Assert.AreEqual(req.SumAssured, result.YearlyTable[0].DeathBenefit4,
            "Year 1 death benefit should equal Sum Assured when FV < SA");
    }

    // -----------------------------------------------------------------------
    // Maturity benefit test
    // -----------------------------------------------------------------------

    [Test]
    public async Task Calculate_MaturityBenefits_MatchFinalYearFundValue()
    {
        var result = await _svc.CalculateAsync(DefaultRequest(pt: 10));
        var lastRow = result.YearlyTable[^1];
        Assert.AreEqual(lastRow.FundValue4, result.MaturityBenefit4);
        Assert.AreEqual(lastRow.FundValue8, result.MaturityBenefit8);
    }

    // -----------------------------------------------------------------------
    // Persist and retrieve test
    // -----------------------------------------------------------------------

    [Test]
    public async Task Calculate_ThenRetrieve_ReturnsSameRows()
    {
        var req = DefaultRequest(policyNumber: "PERSIST-001", pt: 10);
        var calc = await _svc.CalculateAsync(req);

        var retrieved = await _svc.GetByPolicyNumberAsync("PERSIST-001");
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(calc.YearlyTable.Count, retrieved!.YearlyTable.Count);

        for (int i = 0; i < calc.YearlyTable.Count; i++)
        {
            Assert.AreEqual(calc.YearlyTable[i].FundValue4, retrieved.YearlyTable[i].FundValue4, $"Row {i}: FV4");
            Assert.AreEqual(calc.YearlyTable[i].FundValue8, retrieved.YearlyTable[i].FundValue8, $"Row {i}: FV8");
        }
    }

    [Test]
    public async Task GetByPolicyNumber_ReturnsNull_WhenNotFound()
    {
        var result = await _svc.GetByPolicyNumberAsync("NONEXISTENT-99999");
        Assert.IsNull(result);
    }

    // -----------------------------------------------------------------------
    // Female vs Male — mortality rates differ
    // -----------------------------------------------------------------------

    [Test]
    public async Task Calculate_FemaleMortalityCharge_LowerThanMale()
    {
        var maleResult   = await _svc.CalculateAsync(DefaultRequest(policyNumber: "MALE-TEST",   gender: "Male"));
        var femaleResult = await _svc.CalculateAsync(DefaultRequest(policyNumber: "FEMALE-TEST", gender: "Female"));

        // Female rates seeded at 80% of male → female MC should be lower
        Assert.Less(femaleResult.YearlyTable[0].MortalityCharge,
                    maleResult.YearlyTable[0].MortalityCharge,
                    "Female mortality charge should be lower than male at same age");
    }

    // -----------------------------------------------------------------------
    // IRDAI disclaimer present
    // -----------------------------------------------------------------------

    [Test]
    public async Task Calculate_IrdaiDisclaimer_IsPresent()
    {
        var result = await _svc.CalculateAsync(DefaultRequest());
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.IrdaiDisclaimer));
        StringAssert.Contains("investment risk", result.IrdaiDisclaimer);
    }

    // -----------------------------------------------------------------------
    // Risk preference and allocation rules (backend-driven)
    // -----------------------------------------------------------------------

    [Test]
    public void Calculate_SelfManagedAllocationMustBeMultipleOfFive()
    {
        var req = DefaultRequest();
        req.InvestmentStrategy = "Self-Managed Investment Strategy";
        req.FundAllocations = new List<UlipFundAllocation>
        {
            new() { FundType = "Equity Growth Fund", AllocationPercent = 97 },
            new() { FundType = "Debt Fund", AllocationPercent = 3 },
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _svc.CalculateAsync(req));
        Assert.IsNotNull(ex);
        StringAssert.Contains("multiples of 5%", ex!.Message);
    }

    [Test]
    public void Calculate_AgeBasedBlocksWhenMasterMissing()
    {
        var req = DefaultRequest();
        req.InvestmentStrategy = "Age-based Investment Strategy";
        req.RiskPreference = "Aggressive";
        req.FundAllocations.Clear();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _svc.CalculateAsync(req));
        Assert.IsNotNull(ex);
        StringAssert.Contains("Age-based allocation master is missing", ex!.Message);
    }
}
