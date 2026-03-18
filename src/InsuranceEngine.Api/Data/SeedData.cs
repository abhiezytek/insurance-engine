using InsuranceEngine.Api.Controllers;
using InsuranceEngine.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Data;

public static class SeedData
{
    public static async Task SeedAsync(InsuranceDbContext context)
    {
        // Seed default admin user (independent of insurer seed guard)
        if (!await context.AppUsers.AnyAsync())
        {
            context.AppUsers.Add(new AppUser
            {
                Username = "admin",
                PasswordHash = AuthController.HashPassword("admin123"),
                Role = "Admin",
                CreatedDate = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        if (await context.Insurers.AnyAsync()) return;

        var insurer = new Insurer { Name = "Sample Life Insurance Co.", Code = "SLIC" };
        context.Insurers.Add(insurer);
        await context.SaveChangesAsync();

        var product = new Product
        {
            InsurerId = insurer.Id,
            Name = "Endowment Plan",
            Code = "CENTURY_INCOME",
            ProductType = "Traditional"
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var version = new ProductVersion
        {
            ProductId = product.Id,
            Version = "1.0",
            IsActive = true,
            EffectiveDate = new DateTime(2024, 1, 1)
        };
        context.ProductVersions.Add(version);
        await context.SaveChangesAsync();

        var parameters = new List<ProductParameter>
        {
            new() { ProductVersionId = version.Id, Name = "AP", DataType = "decimal", IsRequired = true, Description = "Annual Premium" },
            new() { ProductVersionId = version.Id, Name = "SA", DataType = "decimal", IsRequired = true, Description = "Sum Assured" },
            new() { ProductVersionId = version.Id, Name = "PPT", DataType = "int", IsRequired = true, Description = "Premium Payment Term" },
            new() { ProductVersionId = version.Id, Name = "PT", DataType = "int", IsRequired = true, Description = "Policy Term" },
            new() { ProductVersionId = version.Id, Name = "Age", DataType = "int", IsRequired = true, Description = "Age at entry" },
            new() { ProductVersionId = version.Id, Name = "TotalPremiumPaid", DataType = "decimal", IsRequired = true, Description = "Total Premium Paid" },
            new() { ProductVersionId = version.Id, Name = "SurrenderValue", DataType = "decimal", IsRequired = true, Description = "Surrender Value" },
        };
        context.ProductParameters.AddRange(parameters);
        await context.SaveChangesAsync();

        var formulas = new List<ProductFormula>
        {
            new() { ProductVersionId = version.Id, Name = "GMB", Expression = "AP * 11.5", ExecutionOrder = 1, Description = "Guaranteed Maturity Benefit" },
            new() { ProductVersionId = version.Id, Name = "GSV", Expression = "GMB * 0.30", ExecutionOrder = 2, Description = "Guaranteed Surrender Value" },
            new() { ProductVersionId = version.Id, Name = "SSV", Expression = "AP * 12", ExecutionOrder = 3, Description = "Special Surrender Value" },
            new() { ProductVersionId = version.Id, Name = "MATURITY_BENEFIT", Expression = "GMB", ExecutionOrder = 4, Description = "Maturity Benefit" },
            new() { ProductVersionId = version.Id, Name = "DEATH_BENEFIT", Expression = "MAX(10*AP, 1.05*TotalPremiumPaid, SurrenderValue)", ExecutionOrder = 5, Description = "Death Benefit" },
        };
        context.ProductFormulas.AddRange(formulas);
        await context.SaveChangesAsync();

        await CenturyIncomeFactorLoader.SeedFromCsvAsync(context);

        if (!await context.LoyaltyFactors.AnyAsync())
        {
            context.LoyaltyFactors.AddRange(
                // PPT=7
                new Models.LoyaltyFactor { Ppt=7, PolicyYearFrom=2, PolicyYearTo=2,   RatePercent=3m },
                new Models.LoyaltyFactor { Ppt=7, PolicyYearFrom=3, PolicyYearTo=3,   RatePercent=6m },
                new Models.LoyaltyFactor { Ppt=7, PolicyYearFrom=4, PolicyYearTo=4,   RatePercent=9m },
                new Models.LoyaltyFactor { Ppt=7, PolicyYearFrom=5, PolicyYearTo=5,   RatePercent=12m },
                new Models.LoyaltyFactor { Ppt=7, PolicyYearFrom=6, PolicyYearTo=6,   RatePercent=15m },
                new Models.LoyaltyFactor { Ppt=7, PolicyYearFrom=7, PolicyYearTo=null, RatePercent=18m },
                // PPT=10
                new Models.LoyaltyFactor { Ppt=10, PolicyYearFrom=2,  PolicyYearTo=2,   RatePercent=2m },
                new Models.LoyaltyFactor { Ppt=10, PolicyYearFrom=3,  PolicyYearTo=3,   RatePercent=4m },
                new Models.LoyaltyFactor { Ppt=10, PolicyYearFrom=4,  PolicyYearTo=4,   RatePercent=6m },
                new Models.LoyaltyFactor { Ppt=10, PolicyYearFrom=5,  PolicyYearTo=5,   RatePercent=8m },
                new Models.LoyaltyFactor { Ppt=10, PolicyYearFrom=6,  PolicyYearTo=6,   RatePercent=10m },
                new Models.LoyaltyFactor { Ppt=10, PolicyYearFrom=7,  PolicyYearTo=7,   RatePercent=12m },
                new Models.LoyaltyFactor { Ppt=10, PolicyYearFrom=8,  PolicyYearTo=8,   RatePercent=14m },
                new Models.LoyaltyFactor { Ppt=10, PolicyYearFrom=9,  PolicyYearTo=9,   RatePercent=16m },
                new Models.LoyaltyFactor { Ppt=10, PolicyYearFrom=10, PolicyYearTo=10,  RatePercent=18m },
                new Models.LoyaltyFactor { Ppt=10, PolicyYearFrom=11, PolicyYearTo=null, RatePercent=20m },
                // PPT=12
                new Models.LoyaltyFactor { Ppt=12, PolicyYearFrom=2,  PolicyYearTo=2,   RatePercent=2m },
                new Models.LoyaltyFactor { Ppt=12, PolicyYearFrom=3,  PolicyYearTo=3,   RatePercent=4m },
                new Models.LoyaltyFactor { Ppt=12, PolicyYearFrom=4,  PolicyYearTo=4,   RatePercent=6m },
                new Models.LoyaltyFactor { Ppt=12, PolicyYearFrom=5,  PolicyYearTo=5,   RatePercent=8m },
                new Models.LoyaltyFactor { Ppt=12, PolicyYearFrom=6,  PolicyYearTo=6,   RatePercent=10m },
                new Models.LoyaltyFactor { Ppt=12, PolicyYearFrom=7,  PolicyYearTo=7,   RatePercent=12m },
                new Models.LoyaltyFactor { Ppt=12, PolicyYearFrom=8,  PolicyYearTo=8,   RatePercent=14m },
                new Models.LoyaltyFactor { Ppt=12, PolicyYearFrom=9,  PolicyYearTo=9,   RatePercent=16m },
                new Models.LoyaltyFactor { Ppt=12, PolicyYearFrom=10, PolicyYearTo=10,  RatePercent=18m },
                new Models.LoyaltyFactor { Ppt=12, PolicyYearFrom=11, PolicyYearTo=11,  RatePercent=20m },
                new Models.LoyaltyFactor { Ppt=12, PolicyYearFrom=12, PolicyYearTo=12,  RatePercent=22m },
                new Models.LoyaltyFactor { Ppt=12, PolicyYearFrom=13, PolicyYearTo=null, RatePercent=24m },
                // PPT=15
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=2,  PolicyYearTo=2,   RatePercent=1.5m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=3,  PolicyYearTo=3,   RatePercent=3m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=4,  PolicyYearTo=4,   RatePercent=4.5m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=5,  PolicyYearTo=5,   RatePercent=6m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=6,  PolicyYearTo=6,   RatePercent=7.5m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=7,  PolicyYearTo=7,   RatePercent=9m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=8,  PolicyYearTo=8,   RatePercent=10.5m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=9,  PolicyYearTo=9,   RatePercent=12m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=10, PolicyYearTo=10,  RatePercent=13.5m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=11, PolicyYearTo=11,  RatePercent=15m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=12, PolicyYearTo=12,  RatePercent=16.5m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=13, PolicyYearTo=13,  RatePercent=18m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=14, PolicyYearTo=14,  RatePercent=19.5m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=15, PolicyYearTo=15,  RatePercent=21m },
                new Models.LoyaltyFactor { Ppt=15, PolicyYearFrom=16, PolicyYearTo=null, RatePercent=22.5m }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.DeferredIncomeFactors.AnyAsync())
        {
            context.DeferredIncomeFactors.AddRange(
                // PPT=7, PT=15
                new Models.DeferredIncomeFactor { Ppt=7,  Pt=15, PolicyYear=8,  RatePercent=30m },
                new Models.DeferredIncomeFactor { Ppt=7,  Pt=15, PolicyYear=9,  RatePercent=33m },
                new Models.DeferredIncomeFactor { Ppt=7,  Pt=15, PolicyYear=10, RatePercent=36m },
                new Models.DeferredIncomeFactor { Ppt=7,  Pt=15, PolicyYear=11, RatePercent=39m },
                new Models.DeferredIncomeFactor { Ppt=7,  Pt=15, PolicyYear=12, RatePercent=42m },
                new Models.DeferredIncomeFactor { Ppt=7,  Pt=15, PolicyYear=13, RatePercent=45m },
                new Models.DeferredIncomeFactor { Ppt=7,  Pt=15, PolicyYear=14, RatePercent=48m },
                new Models.DeferredIncomeFactor { Ppt=7,  Pt=15, PolicyYear=15, RatePercent=51m },
                // PPT=10, PT=20
                new Models.DeferredIncomeFactor { Ppt=10, Pt=20, PolicyYear=11, RatePercent=30m },
                new Models.DeferredIncomeFactor { Ppt=10, Pt=20, PolicyYear=12, RatePercent=33m },
                new Models.DeferredIncomeFactor { Ppt=10, Pt=20, PolicyYear=13, RatePercent=36m },
                new Models.DeferredIncomeFactor { Ppt=10, Pt=20, PolicyYear=14, RatePercent=39m },
                new Models.DeferredIncomeFactor { Ppt=10, Pt=20, PolicyYear=15, RatePercent=42m },
                new Models.DeferredIncomeFactor { Ppt=10, Pt=20, PolicyYear=16, RatePercent=45m },
                new Models.DeferredIncomeFactor { Ppt=10, Pt=20, PolicyYear=17, RatePercent=48m },
                new Models.DeferredIncomeFactor { Ppt=10, Pt=20, PolicyYear=18, RatePercent=51m },
                new Models.DeferredIncomeFactor { Ppt=10, Pt=20, PolicyYear=19, RatePercent=54m },
                new Models.DeferredIncomeFactor { Ppt=10, Pt=20, PolicyYear=20, RatePercent=57m },
                // PPT=12, PT=25
                new Models.DeferredIncomeFactor { Ppt=12, Pt=25, PolicyYear=13, RatePercent=30m },
                new Models.DeferredIncomeFactor { Ppt=12, Pt=25, PolicyYear=14, RatePercent=33m },
                new Models.DeferredIncomeFactor { Ppt=12, Pt=25, PolicyYear=15, RatePercent=36m },
                new Models.DeferredIncomeFactor { Ppt=12, Pt=25, PolicyYear=16, RatePercent=39m },
                new Models.DeferredIncomeFactor { Ppt=12, Pt=25, PolicyYear=17, RatePercent=42m },
                new Models.DeferredIncomeFactor { Ppt=12, Pt=25, PolicyYear=18, RatePercent=45m },
                new Models.DeferredIncomeFactor { Ppt=12, Pt=25, PolicyYear=19, RatePercent=48m },
                new Models.DeferredIncomeFactor { Ppt=12, Pt=25, PolicyYear=20, RatePercent=51m },
                new Models.DeferredIncomeFactor { Ppt=12, Pt=25, PolicyYear=21, RatePercent=54m },
                new Models.DeferredIncomeFactor { Ppt=12, Pt=25, PolicyYear=22, RatePercent=57m },
                new Models.DeferredIncomeFactor { Ppt=12, Pt=25, PolicyYear=23, RatePercent=60m },
                new Models.DeferredIncomeFactor { Ppt=12, Pt=25, PolicyYear=24, RatePercent=63m },
                new Models.DeferredIncomeFactor { Ppt=12, Pt=25, PolicyYear=25, RatePercent=66m },
                // PPT=15, PT=25
                new Models.DeferredIncomeFactor { Ppt=15, Pt=25, PolicyYear=16, RatePercent=30m },
                new Models.DeferredIncomeFactor { Ppt=15, Pt=25, PolicyYear=17, RatePercent=33m },
                new Models.DeferredIncomeFactor { Ppt=15, Pt=25, PolicyYear=18, RatePercent=36m },
                new Models.DeferredIncomeFactor { Ppt=15, Pt=25, PolicyYear=19, RatePercent=39m },
                new Models.DeferredIncomeFactor { Ppt=15, Pt=25, PolicyYear=20, RatePercent=42m },
                new Models.DeferredIncomeFactor { Ppt=15, Pt=25, PolicyYear=21, RatePercent=45m },
                new Models.DeferredIncomeFactor { Ppt=15, Pt=25, PolicyYear=22, RatePercent=48m },
                new Models.DeferredIncomeFactor { Ppt=15, Pt=25, PolicyYear=23, RatePercent=51m },
                new Models.DeferredIncomeFactor { Ppt=15, Pt=25, PolicyYear=24, RatePercent=54m },
                new Models.DeferredIncomeFactor { Ppt=15, Pt=25, PolicyYear=25, RatePercent=57m }
            );
            await context.SaveChangesAsync();
        }

        // Seed ULIP product and default data
        await SeedUlipAsync(context);
    }

    private static async Task SeedUlipAsync(InsuranceDbContext context)
    {
        // Only seed once
        if (await context.Products.AnyAsync(p => p.ProductType == "ULIP")) return;

        // Re-use the first insurer
        var insurer = await context.Insurers.FirstAsync();

        var ulipProduct = new Product
        {
            InsurerId   = insurer.Id,
            Name        = "ULIP",
            Code        = "EWEALTH-ROYALE",
            ProductType = "ULIP",
        };
        context.Products.Add(ulipProduct);
        await context.SaveChangesAsync();

        // Default ULIP charges:
        //   PremiumAllocation (PAC) = 0% of premium (no allocation charge)
        //   Policy Administration Charge = ₹100/month (first 10 policy years)
        //   FMC (Fund Management Charge) = 0.1118% of fund per month (Self-Managed strategy)
        context.UlipCharges.AddRange(
            new UlipCharge { ProductId = ulipProduct.Id, ChargeType = "PremiumAllocation", ChargeValue = 0m,      ChargeFrequency = "PercentOfPremium" },
            new UlipCharge { ProductId = ulipProduct.Id, ChargeType = "FMC",               ChargeValue = 0.1118m, ChargeFrequency = "PercentOfFundMonthly" },
            new UlipCharge { ProductId = ulipProduct.Id, ChargeType = "PolicyAdmin",        ChargeValue = 100m,    ChargeFrequency = "Monthly"          }
        );
        await context.SaveChangesAsync();

        // IALM 2012-14 Ultimate mortality rates for both genders
        // Male rates sourced from product workbook (ages 37–46 extracted; others from IALM 2012-14 published table).
        // Female rates approximated at 80% of male (conservative; upload actual table via /api/ulip/upload-mortality).
        if (!await context.MortalityRates.AnyAsync())
        {
            var maleRates = new (int Age, decimal Rate)[]
            {
                // SUD Life e-Wealth Royale product mortality rates (per 1000 per year)
                // Sourced from uploaded commission/mortality CSV reference table
                (0,  5.01m), (1,  4.10m), (2,  1.10m), (3,  0.56m), (4,  0.33m),
                (5,  0.22m), (6,  0.18m), (7,  0.18m), (8,  0.20m), (9,  0.25m),
                (10, 0.32m), (11, 0.41m), (12, 0.52m), (13, 0.63m), (14, 0.74m),
                (15, 0.84m), (16, 0.92m), (17, 1.00m), (18, 1.05m), (19, 1.09m),
                (20, 1.11m), (21, 1.12m), (22, 1.12m), (23, 1.12m), (24, 1.12m),
                (25, 1.12m), (26, 1.12m), (27, 1.12m), (28, 1.13m), (29, 1.15m),
                (30, 1.17m), (31, 1.21m), (32, 1.25m), (33, 1.30m), (34, 1.37m),
                (35, 1.44m), (36, 1.53m), (37, 1.63m), (38, 1.74m), (39, 1.87m),
                (40, 2.02m), (41, 2.18m), (42, 2.36m), (43, 2.57m), (44, 2.81m),
                (45, 3.10m), (46, 3.42m), (47, 3.80m), (48, 4.24m), (49, 4.75m),
                (50, 5.32m), (51, 5.96m), (52, 6.66m), (53, 7.41m), (54, 8.20m),
                (55, 9.02m), (56, 9.85m), (57, 10.71m),(58, 11.58m),(59, 12.47m),
                (60, 13.39m),(61, 14.36m),(62, 15.40m),(63, 16.52m),(64, 17.75m),
                (65, 19.12m),(66, 20.65m),(67, 22.36m),(68, 24.29m),(69, 26.45m),
                (70, 28.87m),(71, 31.58m),(72, 34.60m),(73, 37.97m),(74, 41.71m),
                (75, 45.87m),
            };

            var effDate = new DateTime(2024, 1, 1);
            context.MortalityRates.AddRange(
                maleRates.Select(x => new MortalityRate { Age = x.Age, Rate = x.Rate, Gender = "Male", EffectiveDate = effDate }));
            // Female rates: IALM 2012-14 Female rates (approx. 80% of male; replace via upload for exactness)
            context.MortalityRates.AddRange(
                maleRates.Select(x => new MortalityRate { Age = x.Age, Rate = Math.Round(x.Rate * 0.80m, 4), Gender = "Female", EffectiveDate = effDate }));
            await context.SaveChangesAsync();
        }
    }
}
