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

            // All factor values stored as percentages (CSV decimal × 100).
            // Source: docs/annexure2_gsv.csv

            // PPT=7, PT=15
            decimal[] gsv_7_15 = { 0,35,35,50,50,50,50,55.71m,61.43m,67.14m,72.86m,78.57m,84.29m,90,90 };
            for (int i = 0; i < gsv_7_15.Length; i++)
                gsvRows.Add(new Models.GsvFactor { Ppt=7, Pt=15, PolicyYear=i+1, FactorPercent=gsv_7_15[i] });

            // PPT=7, PT=20
            decimal[] gsv_7_20 = { 0,35,35,50,50,50,50,53.33m,56.67m,60,63.33m,66.67m,70,73.33m,76.67m,80,83.33m,86.67m,90,90 };
            for (int i = 0; i < gsv_7_20.Length; i++)
                gsvRows.Add(new Models.GsvFactor { Ppt=7, Pt=20, PolicyYear=i+1, FactorPercent=gsv_7_20[i] });

            // PPT=10, PT=20
            decimal[] gsv_10_20 = { 0,35,35,50,50,50,50,53.33m,56.67m,60,63.33m,66.67m,70,73.33m,76.67m,80,83.33m,86.67m,90,90 };
            for (int i = 0; i < gsv_10_20.Length; i++)
                gsvRows.Add(new Models.GsvFactor { Ppt=10, Pt=20, PolicyYear=i+1, FactorPercent=gsv_10_20[i] });

            // PPT=10, PT=25
            decimal[] gsv_10_25 = { 0,35,35,50,50,50,50,52.35m,54.71m,57.06m,59.41m,61.76m,64.12m,66.47m,68.82m,71.18m,73.53m,75.88m,78.24m,80.59m,82.94m,85.29m,87.65m,90,90 };
            for (int i = 0; i < gsv_10_25.Length; i++)
                gsvRows.Add(new Models.GsvFactor { Ppt=10, Pt=25, PolicyYear=i+1, FactorPercent=gsv_10_25[i] });

            // PPT=12, PT=25
            decimal[] gsv_12_25 = { 0,35,35,50,50,50,50,52.35m,54.71m,57.06m,59.41m,61.76m,64.12m,66.47m,68.82m,71.18m,73.53m,75.88m,78.24m,80.59m,82.94m,85.29m,87.65m,90,90 };
            for (int i = 0; i < gsv_12_25.Length; i++)
                gsvRows.Add(new Models.GsvFactor { Ppt=12, Pt=25, PolicyYear=i+1, FactorPercent=gsv_12_25[i] });

            context.GsvFactors.AddRange(gsvRows);
            await context.SaveChangesAsync();
        }

        if (!await context.SsvFactors.AnyAsync())
        {
            var ssvRows = new List<Models.SsvFactor>();

            // Factor values stored as percentages (CSV decimal × 100).
            // Source: docs/annexure3_ssv.csv
            // Factor1 is common across options; Factor2 differs per income option.
            // Rows are per (PPT, PT, Option, PolicyYear).

            // --- Helper: add rows for one (PPT, PT, Option) combination ---
            void AddSsvRows(int ppt, int pt, string option, decimal[] f1, decimal[] f2)
            {
                for (int i = 0; i < f1.Length; i++)
                    if (f1[i] != 0 || f2[i] != 0)
                        ssvRows.Add(new Models.SsvFactor { Ppt=ppt, Pt=pt, Option=option, PolicyYear=i+1, Factor1=f1[i], Factor2=f2[i] });
            }

            // ── PPT=7, PT=15 ────────────────────────────────────────────
            decimal[] f1_7_15  = { 0,37.13m,39.82m,42.70m,45.80m,49.13m,52.71m,56.56m,60.69m,65.13m,69.90m,75.05m,80.59m,86.57m,93.02m };
            decimal[] f2i_7_15 = { 0,835.88m,799.98m,761.46m,720.11m,675.72m,628.06m,576.88m,521.90m,462.83m,399.31m,330.97m,257.38m,178.06m,92.46m };
            decimal[] f2d_7_15 = { 0,483.79m,520.89m,560.92m,604.12m,650.78m,701.18m,755.69m,714.67m,660.71m,592.79m,509.79m,410.47m,293.45m,157.19m };
            decimal[] f2t_7_15 = { 0,241.67m,260.21m,280.20m,301.78m,225.09m,142.52m,153.60m,165.59m,178.58m,92.65m,0,0,0,0 };
            AddSsvRows(7, 15, "Immediate", f1_7_15, f2i_7_15);
            AddSsvRows(7, 15, "Deferred",  f1_7_15, f2d_7_15);
            AddSsvRows(7, 15, "Twin",      f1_7_15, f2t_7_15);

            // ── PPT=7, PT=20 ────────────────────────────────────────────
            decimal[] f1_7_20  = { 0,26.74m,28.63m,30.66m,32.84m,35.16m,37.66m,40.33m,43.20m,46.27m,49.56m,53.09m,56.88m,60.95m,65.33m,70.05m,75.14m,80.64m,86.59m,93.02m };
            decimal[] f2i_7_20 = { 0,973.29m,947.94m,920.78m,891.70m,860.56m,827.22m,791.52m,753.30m,712.37m,668.52m,621.53m,571.12m,517.00m,458.83m,396.23m,328.76m,255.96m,177.30m,92.19m };
            decimal[] f2d_7_20 = { 0,756.41m,814.42m,877.01m,944.56m,1017.50m,1096.31m,1181.53m,1173.75m,1155.79m,1126.90m,1086.25m,1032.92m,965.91m,884.05m,786.10m,670.65m,536.13m,380.83m,202.82m };
            decimal[] f2t_7_20 = { 0,308.40m,332.05m,357.57m,385.11m,314.85m,239.24m,257.84m,277.96m,299.76m,223.38m,141.10m,152.36m,164.60m,177.91m,92.41m,0,0,0,0 };
            AddSsvRows(7, 20, "Immediate", f1_7_20, f2i_7_20);
            AddSsvRows(7, 20, "Deferred",  f1_7_20, f2d_7_20);
            AddSsvRows(7, 20, "Twin",      f1_7_20, f2t_7_20);

            // ── PPT=10, PT=20 ────────────────────────────────────────────
            decimal[] f1_10_20 = { 0,26.74m,28.63m,30.66m,32.84m,35.16m,37.66m,40.33m,43.20m,46.27m,49.56m,53.09m,56.88m,60.95m,65.33m,70.05m,75.14m,80.64m,86.59m,93.02m };
            decimal[] f2i_10_20= { 0,973.29m,947.94m,920.78m,891.70m,860.56m,827.22m,791.52m,753.30m,712.37m,668.52m,621.53m,571.12m,517.00m,458.83m,396.23m,328.76m,255.96m,177.30m,92.19m };
            decimal[] f2d_10_20= { 0,472.83m,509.10m,548.22m,590.44m,636.04m,685.30m,738.57m,796.22m,858.65m,926.34m,899.79m,861.59m,810.80m,746.40m,667.24m,572.02m,459.35m,327.64m,175.17m };
            decimal[] f2t_10_20= { 0,245.03m,263.82m,284.09m,305.97m,329.60m,355.13m,382.74m,312.61m,237.12m,255.81m,276.10m,298.13m,222.08m,140.05m,151.46m,163.90m,177.46m,92.25m,0 };
            AddSsvRows(10, 20, "Immediate", f1_10_20, f2i_10_20);
            AddSsvRows(10, 20, "Deferred",  f1_10_20, f2d_10_20);
            AddSsvRows(10, 20, "Twin",      f1_10_20, f2t_10_20);

            // ── PPT=10, PT=25 ────────────────────────────────────────────
            decimal[] f1_10_25 = { 0,19.82m,21.19m,22.64m,24.20m,25.86m,27.63m,29.53m,31.55m,33.70m,36.01m,38.46m,41.08m,43.89m,46.89m,50.11m,53.56m,57.27m,61.27m,65.58m,70.23m,75.26m,80.70m,86.61m,93.02m };
            decimal[] f2i_10_25= { 0,1064.52m,1046.16m,1026.56m,1005.62m,983.28m,959.44m,934.02m,906.92m,878.04m,847.25m,814.43m,779.42m,742.03m,702.07m,659.29m,613.42m,564.17m,511.20m,454.15m,392.61m,326.14m,254.23m,176.34m,91.84m };
            decimal[] f2d_10_25= { 0,672.01m,723.55m,779.16m,839.17m,903.97m,973.99m,1049.70m,1131.63m,1220.37m,1316.56m,1320.96m,1316.36m,1302.11m,1277.46m,1241.58m,1193.52m,1132.26m,1056.65m,965.42m,857.18m,730.38m,583.30m,414.03m,220.41m };
            decimal[] f2t_10_25= { 0,279.83m,301.29m,324.44m,349.43m,376.42m,405.57m,437.10m,371.21m,300.32m,324.00m,349.69m,377.59m,307.92m,232.84m,251.81m,272.49m,295.03m,219.63m,138.08m,149.77m,162.56m,176.57m,91.92m,0 };
            AddSsvRows(10, 25, "Immediate", f1_10_25, f2i_10_25);
            AddSsvRows(10, 25, "Deferred",  f1_10_25, f2d_10_25);
            AddSsvRows(10, 25, "Twin",      f1_10_25, f2t_10_25);

            // ── PPT=12, PT=25 ────────────────────────────────────────────
            decimal[] f1_12_25 = { 0,19.82m,21.19m,22.64m,24.20m,25.86m,27.63m,29.53m,31.55m,33.70m,36.01m,38.46m,41.08m,43.89m,46.89m,50.11m,53.56m,57.27m,61.27m,65.58m,70.23m,75.26m,80.70m,86.61m,93.02m };
            decimal[] f2i_12_25= { 0,1064.52m,1046.16m,1026.56m,1005.62m,983.28m,959.44m,934.02m,906.92m,878.04m,847.25m,814.43m,779.42m,742.03m,702.07m,659.29m,613.42m,564.17m,511.20m,454.15m,392.61m,326.14m,254.23m,176.34m,91.84m };
            decimal[] f2d_12_25= { 0,508.27m,547.25m,589.31m,634.69m,683.71m,736.66m,793.93m,855.89m,923.01m,995.77m,1074.72m,1160.48m,1154.80m,1137.05m,1109.72m,1071.52m,1019.43m,951.22m,874.59m,778.66m,665.15m,532.45m,379.76m,202.04m };
            decimal[] f2t_12_25= { 0,209.71m,225.79m,243.15m,261.87m,282.10m,303.95m,327.57m,353.14m,380.83m,310.85m,235.50m,254.29m,274.72m,296.95m,221.15m,139.31m,150.83m,163.40m,177.13m,92.13m,92.21m,0,0,0 };
            AddSsvRows(12, 25, "Immediate", f1_12_25, f2i_12_25);
            AddSsvRows(12, 25, "Deferred",  f1_12_25, f2d_12_25);
            AddSsvRows(12, 25, "Twin",      f1_12_25, f2t_12_25);

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

        // Default ULIP charges — PAC 0%, Policy Admin ₹100/month, FMC 0.1118% monthly
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
                // IALM 2012-14 Ultimate Male (per 1000 per year)
                (0,  5.15m), (1,  1.15m), (2,  0.85m), (3,  0.69m), (4,  0.60m),
                (5,  0.54m), (6,  0.48m), (7,  0.43m), (8,  0.40m), (9,  0.38m),
                (10, 0.36m), (11, 0.35m), (12, 0.36m), (13, 0.40m), (14, 0.46m),
                (15, 0.55m), (16, 0.65m), (17, 0.77m), (18, 0.89m), (19, 1.01m),
                (20, 1.11m), (21, 1.19m), (22, 1.25m), (23, 1.29m), (24, 1.32m),
                (25, 1.34m), (26, 1.36m), (27, 1.38m), (28, 1.40m), (29, 1.43m),
                (30, 1.46m), (31, 1.50m), (32, 1.53m), (33, 1.56m), (34, 1.58m),
                (35, 1.59m), (36, 1.61m),
                // Workbook-extracted exact rates for key ages
                (37, 1.6298m), (38, 1.7440m), (39, 1.8722m),
                (40, 2.0160m), (41, 2.1780m), (42, 2.3628m),
                (43, 2.5730m), (44, 2.8143m), (45, 3.0772m), (46, 3.4188m),
                // IALM 2012-14 continuation
                (47, 3.78m),  (48, 4.18m),  (49, 4.60m),
                (50, 5.06m),  (51, 5.55m),  (52, 6.06m),  (53, 6.60m),  (54, 7.17m),
                (55, 7.77m),  (56, 8.41m),  (57, 9.09m),  (58, 9.81m),  (59, 10.57m),
                (60, 11.37m), (61, 12.22m), (62, 13.12m), (63, 14.07m), (64, 15.07m),
                (65, 16.13m), (66, 17.24m), (67, 18.41m), (68, 19.65m), (69, 20.95m),
                (70, 22.31m), (71, 23.75m), (72, 25.26m), (73, 26.84m), (74, 28.50m),
                (75, 30.25m),
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
