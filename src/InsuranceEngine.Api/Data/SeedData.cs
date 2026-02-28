using InsuranceEngine.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Data;

public static class SeedData
{
    public static async Task SeedAsync(InsuranceDbContext context)
    {
        if (await context.Insurers.AnyAsync()) return;

        var insurer = new Insurer { Name = "Sample Life Insurance Co.", Code = "SLIC" };
        context.Insurers.Add(insurer);
        await context.SaveChangesAsync();

        var product = new Product
        {
            InsurerId = insurer.Id,
            Name = "Century Income Plan",
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

        // Seed factor tables only if empty
        if (!await context.GmbFactors.AnyAsync())
        {
            context.GmbFactors.AddRange(
                new Models.GmbFactor { Ppt=7,  Pt=15, EntryAgeMin=0,  EntryAgeMax=40, Option="Immediate", Factor=11.50m },
                new Models.GmbFactor { Ppt=7,  Pt=15, EntryAgeMin=41, EntryAgeMax=65, Option="Immediate", Factor=10.80m },
                new Models.GmbFactor { Ppt=7,  Pt=15, EntryAgeMin=0,  EntryAgeMax=40, Option="Deferred",  Factor=12.50m },
                new Models.GmbFactor { Ppt=7,  Pt=15, EntryAgeMin=41, EntryAgeMax=65, Option="Deferred",  Factor=11.80m },
                new Models.GmbFactor { Ppt=7,  Pt=15, EntryAgeMin=0,  EntryAgeMax=40, Option="Twin",      Factor=13.00m },
                new Models.GmbFactor { Ppt=7,  Pt=15, EntryAgeMin=41, EntryAgeMax=65, Option="Twin",      Factor=12.30m },
                new Models.GmbFactor { Ppt=10, Pt=20, EntryAgeMin=0,  EntryAgeMax=40, Option="Immediate", Factor=12.00m },
                new Models.GmbFactor { Ppt=10, Pt=20, EntryAgeMin=41, EntryAgeMax=65, Option="Immediate", Factor=11.20m },
                new Models.GmbFactor { Ppt=10, Pt=20, EntryAgeMin=0,  EntryAgeMax=40, Option="Deferred",  Factor=13.50m },
                new Models.GmbFactor { Ppt=10, Pt=20, EntryAgeMin=41, EntryAgeMax=65, Option="Deferred",  Factor=12.80m },
                new Models.GmbFactor { Ppt=10, Pt=20, EntryAgeMin=0,  EntryAgeMax=40, Option="Twin",      Factor=14.00m },
                new Models.GmbFactor { Ppt=10, Pt=20, EntryAgeMin=41, EntryAgeMax=65, Option="Twin",      Factor=13.20m },
                new Models.GmbFactor { Ppt=12, Pt=25, EntryAgeMin=0,  EntryAgeMax=40, Option="Immediate", Factor=13.00m },
                new Models.GmbFactor { Ppt=12, Pt=25, EntryAgeMin=41, EntryAgeMax=65, Option="Immediate", Factor=12.20m },
                new Models.GmbFactor { Ppt=15, Pt=25, EntryAgeMin=0,  EntryAgeMax=40, Option="Immediate", Factor=14.00m },
                new Models.GmbFactor { Ppt=15, Pt=25, EntryAgeMin=41, EntryAgeMax=65, Option="Immediate", Factor=13.20m }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.GsvFactors.AnyAsync())
        {
            var gsvRows = new List<Models.GsvFactor>();
            // PPT=7
            var gsv7 = new[] { 0m,30m,35m,40m,45m,50m,55m,58m,61m,64m,67m,70m,75m,80m,90m };
            for (int i = 0; i < gsv7.Length; i++)
                gsvRows.Add(new Models.GsvFactor { Ppt=7, PolicyYear=i+1, FactorPercent=gsv7[i] });
            // PPT=10
            var gsv10 = new[] { 0m,0m,30m,35m,40m,45m,50m,55m,57m,60m,63m,66m,69m,72m,75m,78m,81m,84m,87m,90m };
            for (int i = 0; i < gsv10.Length; i++)
                gsvRows.Add(new Models.GsvFactor { Ppt=10, PolicyYear=i+1, FactorPercent=gsv10[i] });
            // PPT=12
            var gsv12 = new[] { 0m,0m,0m,30m,35m,40m,45m,50m,55m,60m,62m,65m,68m,71m,74m,77m,80m,83m,86m,89m,91m,93m,95m,97m,100m };
            for (int i = 0; i < gsv12.Length; i++)
                gsvRows.Add(new Models.GsvFactor { Ppt=12, PolicyYear=i+1, FactorPercent=gsv12[i] });
            // PPT=15
            var gsv15 = new[] { 0m,0m,0m,0m,30m,35m,40m,45m,50m,55m,58m,61m,64m,67m,70m,73m,76m,79m,82m,85m,88m,91m,93m,96m,100m };
            for (int i = 0; i < gsv15.Length; i++)
                gsvRows.Add(new Models.GsvFactor { Ppt=15, PolicyYear=i+1, FactorPercent=gsv15[i] });
            context.GsvFactors.AddRange(gsvRows);
            await context.SaveChangesAsync();
        }

        if (!await context.SsvFactors.AnyAsync())
        {
            var ssvRows = new List<Models.SsvFactor>();
            // PPT=7
            decimal[] ssv7f1 = { 0,40,45,50,55,60,65,68,71,74,77,80,84,90,100 };
            decimal[] ssv7f2 = { 0,20,25,30,35,40,45,48,51,54,57,60,64,70,80 };
            for (int i = 0; i < ssv7f1.Length; i++)
                ssvRows.Add(new Models.SsvFactor { Ppt=7, PolicyYear=i+1, Factor1=ssv7f1[i], Factor2=ssv7f2[i] });
            // PPT=10
            decimal[] ssv10f1 = { 0,0,35,40,45,50,55,60,63,66,69,72,75,78,81,84,87,90,95,100 };
            decimal[] ssv10f2 = { 0,0,15,20,25,30,35,40,43,46,49,52,55,58,61,64,67,70,75,80 };
            for (int i = 0; i < ssv10f1.Length; i++)
                ssvRows.Add(new Models.SsvFactor { Ppt=10, PolicyYear=i+1, Factor1=ssv10f1[i], Factor2=ssv10f2[i] });
            // PPT=12
            decimal[] ssv12f1 = { 0,0,0,35,40,45,50,55,60,63,66,69,72,75,78,81,84,87,90,93,95,97,98,99,100 };
            decimal[] ssv12f2 = { 0,0,0,15,20,25,30,35,40,43,46,49,52,55,58,61,64,67,70,73,75,77,78,79,80 };
            for (int i = 0; i < ssv12f1.Length; i++)
                ssvRows.Add(new Models.SsvFactor { Ppt=12, PolicyYear=i+1, Factor1=ssv12f1[i], Factor2=ssv12f2[i] });
            // PPT=15
            decimal[] ssv15f1 = { 0,0,0,0,35,40,45,50,55,60,63,66,69,72,75,78,81,84,87,90,92,94,96,98,100 };
            decimal[] ssv15f2 = { 0,0,0,0,15,20,25,30,35,40,43,46,49,52,55,58,61,64,67,70,72,74,76,78,80 };
            for (int i = 0; i < ssv15f1.Length; i++)
                ssvRows.Add(new Models.SsvFactor { Ppt=15, PolicyYear=i+1, Factor1=ssv15f1[i], Factor2=ssv15f2[i] });
            context.SsvFactors.AddRange(ssvRows);
            await context.SaveChangesAsync();
        }

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
    }
}
