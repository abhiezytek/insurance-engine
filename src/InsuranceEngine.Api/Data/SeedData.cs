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
    }
}
