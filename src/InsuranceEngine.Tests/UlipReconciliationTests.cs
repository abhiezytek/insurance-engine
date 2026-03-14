using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InsuranceEngine.Tests;

/// <summary>
/// Reconciliation tests for the ULIP monthly projection engine.
/// Reference values sourced from the SUD Life e-Wealth Royale product workbook:
///   - ulip_parta.csv  (Part A yearly summary)
///   - ulip_partb.csv  (Part B 8% detailed table)
///   - ulip_bi4.csv    (monthly 4% detail)
///   - ulip_bi8.csv    (monthly 8% detail)
///
/// Test inputs (matching the reference workbook):
///   Option = Platinum, Age = 37, PT = 20, PPT = 10,
///   AP = ₹24,000, SA = ₹2,40,000, Mode = Yearly,
///   DOB = 8-Jul-1988 → Policy starts March (months_since_birthday = 8).
///
/// Tolerance: ±25 rupees (to account for per-step rounding in the workbook vs
/// no-intermediate-rounding in this implementation, accumulated over 20 years × 12 months).
/// </summary>
[TestFixture]
public class UlipReconciliationTests
{
    private UlipCalculationService _svc = null!;
    private InsuranceDbContext _db = null!;
    private const decimal Tolerance = 25m;   // ±₹25 tolerance vs workbook CSV values

    [OneTimeSetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase("UlipReconTestDb_" + Guid.NewGuid())
            .Options;
        _db = new InsuranceDbContext(options);

        // Seed product
        var insurer = new Insurer { Id = 1, Name = "SUD Life", Code = "SUDLIFE" };
        _db.Insurers.Add(insurer);
        var product = new Product { Id = 10, InsurerId = 1, Name = "e-Wealth Royale", Code = "EWEALTH-ROYALE", ProductType = "ULIP" };
        _db.Products.Add(product);

        // Seed IALM 2012-14 Male mortality rates (sourced from workbook back-calculation)
        var ialmMaleRates = new (int Age, decimal Rate)[]
        {
            (0,  5.15m), (1,  1.15m), (2,  0.85m), (3,  0.69m), (4,  0.60m),
            (5,  0.54m), (6,  0.48m), (7,  0.43m), (8,  0.40m), (9,  0.38m),
            (10, 0.36m), (11, 0.35m), (12, 0.36m), (13, 0.40m), (14, 0.46m),
            (15, 0.55m), (16, 0.65m), (17, 0.77m), (18, 0.89m), (19, 1.01m),
            (20, 1.11m), (21, 1.19m), (22, 1.25m), (23, 1.29m), (24, 1.32m),
            (25, 1.34m), (26, 1.36m), (27, 1.38m), (28, 1.40m), (29, 1.43m),
            (30, 1.46m), (31, 1.50m), (32, 1.53m), (33, 1.56m), (34, 1.58m),
            (35, 1.59m), (36, 1.61m),
            (37, 1.6298m), (38, 1.7440m), (39, 1.8722m),
            (40, 2.0160m), (41, 2.1780m), (42, 2.3628m),
            (43, 2.5730m), (44, 2.8143m), (45, 3.0772m), (46, 3.4188m),
            (47, 3.78m),  (48, 4.18m),  (49, 4.60m),
            (50, 5.06m),  (51, 5.55m),  (52, 6.06m),  (53, 6.60m),  (54, 7.17m),
            (55, 7.77m),  (56, 8.41m),  (57, 9.09m),  (58, 9.81m),  (59, 10.57m),
            (60, 11.37m), (61, 12.22m), (62, 13.12m), (63, 14.07m), (64, 15.07m),
            (65, 16.13m), (66, 17.24m), (67, 18.41m), (68, 19.65m), (69, 20.95m),
            (70, 22.31m), (71, 23.75m), (72, 25.26m), (73, 26.84m), (74, 28.50m),
            (75, 30.25m),
        };
        int idCtr = 1000;
        foreach (var (age, rate) in ialmMaleRates)
        {
            _db.MortalityRates.Add(new MortalityRate { Id = idCtr++, Age = age, Rate = rate, Gender = "Male", EffectiveDate = DateTime.UtcNow });
        }

        _db.SaveChanges();
        _svc = new UlipCalculationService(_db);
    }

    [OneTimeTearDown]
    public void TearDown() => _db.Dispose();

    // ---------------------------------------------------------------------------
    // Reference request: matches the workbook scenario exactly
    // ---------------------------------------------------------------------------

    private static UlipCalculationRequest WorkbookRequest() => new()
    {
        PolicyNumber      = "RECON-001",
        CustomerName      = "ABC",
        PolicyholderName  = "XYZ",
        ProductCode       = "EWEALTH-ROYALE",
        Option            = "Platinum",
        Gender            = "Male",
        DateOfBirth       = new DateTime(1988, 7, 8),   // DOB 8-Jul-88 → age 37
        EntryAge          = 37,
        PolicyTerm        = 20,
        Ppt               = 10,
        AnnualizedPremium = 24_000m,
        SumAssured        = 240_000m,
        PremiumFrequency  = "Yearly",
        PolicyEffectiveDate = new DateTime(2026, 3, 14), // March start → birthday in month 5
        FundAllocations   = new List<UlipFundAllocation>
        {
            new() { FundType = "SUD Life Nifty Alpha 50 Index Fund", AllocationPercent = 100 },
        },
    };

    // =========================================================================
    // Part A — Dual-scenario summary
    // =========================================================================

    [Test]
    public async Task PartA_Year1_8pct_FundAtEnd_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartARows.First(r => r.Year == 1);
        // Workbook: 23,951
        Assert.That(row.FundAtEndOfYear8, Is.EqualTo(23951m).Within(Tolerance),
            "Part A Year 1 8%: Fund at End should be ~23,951");
    }

    [Test]
    public async Task PartA_Year1_8pct_SurrenderValue_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartARows.First(r => r.Year == 1);
        // Workbook: 20,951 (Discontinuance Charge = 3,000 for AP ≤ 25,000 in year 1)
        Assert.That(row.SurrenderValue8, Is.EqualTo(20951m).Within(Tolerance),
            "Part A Year 1 8%: Surrender Value should be ~20,951");
    }

    [Test]
    public async Task PartA_Year1_8pct_DeathBenefit_EqualsSumAssured()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartARows.First(r => r.Year == 1);
        // Workbook: 2,40,000 (SA dominates since FV << SA)
        Assert.That(row.DeathBenefit8, Is.EqualTo(240_000m).Within(1m),
            "Part A Year 1 8%: DB should equal SA (2,40,000)");
    }

    [Test]
    public async Task PartA_Year1_8pct_MortalityCharges_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartARows.First(r => r.Year == 1);
        // Workbook: 369 (annual total of monthly MC using IALM 2012-14)
        Assert.That(row.MortalityCharges8, Is.EqualTo(369m).Within(Tolerance),
            "Part A Year 1 8%: Mortality Charges should be ~369");
    }

    [Test]
    public async Task PartA_Year1_8pct_OtherCharges_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartARows.First(r => r.Year == 1);
        // Workbook: 1,522 (PAC=1,200 + FMC=322)
        Assert.That(row.OtherCharges8, Is.EqualTo(1522m).Within(Tolerance),
            "Part A Year 1 8%: Other Charges (PAC+FMC) should be ~1,522");
    }

    [Test]
    public async Task PartA_Year1_4pct_FundAtEnd_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartARows.First(r => r.Year == 1);
        // Workbook: 23,036
        Assert.That(row.FundAtEndOfYear4, Is.EqualTo(23036m).Within(Tolerance),
            "Part A Year 1 4%: Fund at End should be ~23,036");
    }

    [Test]
    public async Task PartA_Year1_4pct_SurrenderValue_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartARows.First(r => r.Year == 1);
        // Workbook: 20,036
        Assert.That(row.SurrenderValue4, Is.EqualTo(20036m).Within(Tolerance),
            "Part A Year 1 4%: Surrender Value should be ~20,036");
    }

    [Test]
    public async Task PartA_Year10_8pct_FundAtEnd_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartARows.First(r => r.Year == 10);
        // Workbook: 3,48,537
        Assert.That(row.FundAtEndOfYear8, Is.EqualTo(348537m).Within(Tolerance),
            "Part A Year 10 8%: Fund should be ~3,48,537");
    }

    [Test]
    public async Task PartA_Year20_8pct_FundAtEnd_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartARows.First(r => r.Year == 20);
        // Workbook: 6,97,525
        Assert.That(row.FundAtEndOfYear8, Is.EqualTo(697525m).Within(Tolerance),
            "Part A Year 20 8%: Fund should be ~6,97,525");
    }

    [Test]
    public async Task PartA_Year10_4pct_FundAtEnd_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartARows.First(r => r.Year == 10);
        // Workbook: 2,81,415
        Assert.That(row.FundAtEndOfYear4, Is.EqualTo(281415m).Within(Tolerance),
            "Part A Year 10 4%: Fund should be ~2,81,415");
    }

    [Test]
    public async Task PartA_Year20_4pct_FundAtEnd_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartARows.First(r => r.Year == 20);
        // Workbook: 3,88,072
        Assert.That(row.FundAtEndOfYear4, Is.EqualTo(388072m).Within(Tolerance),
            "Part A Year 20 4%: Fund should be ~3,88,072");
    }

    [Test]
    public async Task PartA_SurrenderCharge_YearsOneToFour_AppliedCorrectly()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row1 = result.PartARows.First(r => r.Year == 1);
        var row2 = result.PartARows.First(r => r.Year == 2);
        var row3 = result.PartARows.First(r => r.Year == 3);
        var row4 = result.PartARows.First(r => r.Year == 4);
        var row5 = result.PartARows.First(r => r.Year == 5);

        // IRDAI DC for AP=24,000: yr1=3000, yr2=2000, yr3=1500, yr4=1000, yr5=0
        Assert.That(row1.FundAtEndOfYear8 - row1.SurrenderValue8, Is.EqualTo(3000m).Within(1m), "Year 1 DC = 3000");
        Assert.That(row2.FundAtEndOfYear8 - row2.SurrenderValue8, Is.EqualTo(2000m).Within(1m), "Year 2 DC = 2000");
        Assert.That(row3.FundAtEndOfYear8 - row3.SurrenderValue8, Is.EqualTo(1500m).Within(1m), "Year 3 DC = 1500");
        Assert.That(row4.FundAtEndOfYear8 - row4.SurrenderValue8, Is.EqualTo(1000m).Within(1m), "Year 4 DC = 1000");
        Assert.That(row5.FundAtEndOfYear8 - row5.SurrenderValue8, Is.EqualTo(0m).Within(1m),    "Year 5 DC = 0");
    }

    // =========================================================================
    // Part B — Detailed table (8% scenario)
    // =========================================================================

    [Test]
    public async Task PartB8_Year1_MortalityCharge_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartBRows8.First(r => r.Year == 1);
        // Workbook Part B: 369
        Assert.That(row.MortalityCharges, Is.EqualTo(369m).Within(Tolerance), "Part B 8% Year 1 MC ≈ 369");
    }

    [Test]
    public async Task PartB8_Year1_PolicyAdminCharge_Is1200()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartBRows8.First(r => r.Year == 1);
        // ₹100/month × 12 = ₹1,200
        Assert.That(row.PolicyAdministrationCharges, Is.EqualTo(1200m).Within(1m), "Part B 8% Year 1 PAC = 1200");
    }

    [Test]
    public async Task PartB8_Year1_FMC_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartBRows8.First(r => r.Year == 1);
        // Workbook: 322
        Assert.That(row.FundManagementCharge, Is.EqualTo(322m).Within(Tolerance), "Part B 8% Year 1 FMC ≈ 322");
    }

    [Test]
    public async Task PartB8_Year6_LoyaltyAddition_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartBRows8.First(r => r.Year == 6);
        // Workbook: 166
        Assert.That(row.LoyaltyAddition, Is.EqualTo(166m).Within(Tolerance), "Part B 8% Year 6 LA ≈ 166");
    }

    [Test]
    public async Task PartB8_Year10_WealthBooster_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartBRows8.First(r => r.Year == 10);
        // Workbook: 8,924
        Assert.That(row.WealthBooster, Is.EqualTo(8924m).Within(Tolerance), "Part B 8% Year 10 WB ≈ 8,924");
    }

    [Test]
    public async Task PartB8_Year10_RoPAC_Is12000()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartBRows8.First(r => r.Year == 10);
        // 10 years × 12 months × ₹100 = ₹12,000
        Assert.That(row.ReturnOfCharges, Is.EqualTo(12000m).Within(1m), "Part B 8% Year 10 RoPAC = 12,000");
    }

    [Test]
    public async Task PartB8_Year10_FundAtEnd_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartBRows8.First(r => r.Year == 10);
        // Workbook: 3,48,537
        Assert.That(row.FundAtEndOfYear, Is.EqualTo(348537m).Within(Tolerance), "Part B 8% Year 10 FV ≈ 3,48,537");
    }

    [Test]
    public async Task PartB8_Year15_WealthBooster_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartBRows8.First(r => r.Year == 15);
        // Workbook: 13,527
        Assert.That(row.WealthBooster, Is.EqualTo(13527m).Within(Tolerance), "Part B 8% Year 15 WB ≈ 13,527");
    }

    [Test]
    public async Task PartB8_Year20_WealthBooster_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartBRows8.First(r => r.Year == 20);
        // Workbook: 19,110
        Assert.That(row.WealthBooster, Is.EqualTo(19110m).Within(Tolerance), "Part B 8% Year 20 WB ≈ 19,110");
    }

    [Test]
    public async Task PartB8_Year20_ReturnOfMortalityCharges_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartBRows8.First(r => r.Year == 20);
        // Workbook: 1,893
        Assert.That(row.ReturnOfCharges, Is.EqualTo(1893m).Within(Tolerance), "Part B 8% Year 20 RoMC ≈ 1,893");
    }

    [Test]
    public async Task PartB8_Year20_FundAtEnd_MatchesWorkbook()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var row = result.PartBRows8.First(r => r.Year == 20);
        // Workbook: 6,97,525
        Assert.That(row.FundAtEndOfYear, Is.EqualTo(697525m).Within(Tolerance), "Part B 8% Year 20 FV ≈ 6,97,525");
    }

    // =========================================================================
    // Structural / formula tests (independent of workbook exact values)
    // =========================================================================

    [Test]
    public async Task PartBRows_HavePremiumOnlyDuringPPT()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        foreach (var r in result.PartBRows8)
        {
            if (r.Year <= 10) Assert.That(r.AnnualizedPremium, Is.EqualTo(24000m), $"Year {r.Year}: AP should be 24000 during PPT");
            else              Assert.That(r.AnnualizedPremium, Is.EqualTo(0m),     $"Year {r.Year}: AP should be 0 after PPT");
        }
    }

    [Test]
    public async Task PartBRows_NoPolicyAdminAfterYear10()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        foreach (var r in result.PartBRows8.Where(r => r.Year > 10))
            Assert.That(r.PolicyAdministrationCharges, Is.EqualTo(0m), $"Year {r.Year}: no PAC after year 10");
    }

    [Test]
    public async Task PartBRows_NoLoyaltyAfterPPT()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        foreach (var r in result.PartBRows8.Where(r => r.Year > 10))
            Assert.That(r.LoyaltyAddition, Is.EqualTo(0m), $"Year {r.Year}: no LA after PPT=10");
    }

    [Test]
    public async Task PartBRows_WealthBoosterOnlyAtMilestoneYears()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var wbYears = new HashSet<int> { 10, 15, 20 };  // for PT=20
        foreach (var r in result.PartBRows8)
        {
            if (wbYears.Contains(r.Year)) Assert.That(r.WealthBooster, Is.GreaterThan(0m), $"Year {r.Year}: WB should be positive");
            else                          Assert.That(r.WealthBooster, Is.EqualTo(0m),      $"Year {r.Year}: WB should be 0");
        }
    }

    [Test]
    public async Task PartA_Has20Rows_ForPT20()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        Assert.That(result.PartARows.Count, Is.EqualTo(20), "Part A should have 20 rows for PT=20");
        Assert.That(result.PartBRows8.Count, Is.EqualTo(20), "Part B (8%) should have 20 rows for PT=20");
        Assert.That(result.PartBRows4.Count, Is.EqualTo(20), "Part B (4%) should have 20 rows for PT=20");
    }

    [Test]
    public async Task MaturityBenefits_MatchLastPartARow()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        var lastRow = result.PartARows.Last();
        Assert.That(result.MaturityBenefit8, Is.EqualTo(lastRow.FundAtEndOfYear8), "Maturity 8% = last Part A row FV8");
        Assert.That(result.MaturityBenefit4, Is.EqualTo(lastRow.FundAtEndOfYear4), "Maturity 4% = last Part A row FV4");
    }

    [Test]
    public async Task DeathBenefit_IsAtLeastSumAssured_AllYears()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        foreach (var row in result.PartARows)
        {
            Assert.That(row.DeathBenefit8, Is.GreaterThanOrEqualTo(240_000m).Or.EqualTo(row.FundAtEndOfYear8),
                $"Year {row.Year}: DB8 ≥ SA or ≥ FV8");
        }
    }

    [Test]
    public async Task FundValue8_GreaterThanOrEqualTo_FundValue4_AllYears()
    {
        var result = await _svc.CalculateAsync(WorkbookRequest());
        foreach (var row in result.PartARows)
            Assert.That(row.FundAtEndOfYear8, Is.GreaterThanOrEqualTo(row.FundAtEndOfYear4),
                $"Year {row.Year}: FV8 ≥ FV4");
    }
}
