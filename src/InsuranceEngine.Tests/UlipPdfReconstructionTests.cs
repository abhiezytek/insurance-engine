using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Text.Json;

namespace InsuranceEngine.Tests;

[TestFixture]
public class UlipPdfReconstructionTests
{
    [Test]
    public async Task GetByPolicyNumber_UsesLoggedSnapshot_ForPartAAndPartBRows()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase($"ulip-pdf-{Guid.NewGuid()}")
            .Options;

        using var db = new InsuranceDbContext(options);

        db.Products.Add(new Product { Id = 1, Code = "EWEALTH-ROYALE", Name = "e-Wealth Royale", ProductType = "ULIP" });
        await db.SaveChangesAsync();

        var service = new UlipCalculationService(db);

        var request = new UlipCalculationRequest
        {
            PolicyNumber = "UL-TEST-001",
            CustomerName = "Test User",
            ProductCode = "EWEALTH-ROYALE",
            EntryAge = 35,
            DateOfBirth = new DateTime(1991, 1, 1),
            PolicyTerm = 20,
            Ppt = 10,
            AnnualizedPremium = 100000m,
            PremiumFrequency = "Yearly",
            FundAllocations = new List<UlipFundAllocation>
            {
                new() { FundType = "Equity Growth Fund", AllocationPercent = 100m }
            }
        };

        var calculated = await service.CalculateAsync(request);
        Assert.That(calculated.PartARows, Is.Not.Empty, "Initial calculation should produce Part A rows");
        Assert.That(calculated.PartBRows4, Is.Not.Empty, "Initial calculation should produce Part B @4% rows");
        Assert.That(calculated.PartBRows8, Is.Not.Empty, "Initial calculation should produce Part B @8% rows");

        var fetched = await service.GetByPolicyNumberAsync(request.PolicyNumber);

        Assert.That(fetched, Is.Not.Null, "Fetched illustration should not be null");
        Assert.That(fetched!.PartARows, Is.Not.Empty, "Fetched illustration should include Part A rows from calculation log");
        Assert.That(fetched.PartBRows4, Is.Not.Empty, "Fetched illustration should include Part B @4% rows from calculation log");
        Assert.That(fetched.PartBRows8, Is.Not.Empty, "Fetched illustration should include Part B @8% rows from calculation log");
    }

    [Test]
    public async Task CalculationLog_SanitizesSensitiveFields()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase($"ulip-pii-{Guid.NewGuid()}")
            .Options;

        using var db = new InsuranceDbContext(options);
        db.Products.Add(new Product { Id = 1, Code = "EWEALTH-ROYALE", Name = "e-Wealth Royale", ProductType = "ULIP" });
        await db.SaveChangesAsync();

        var service = new UlipCalculationService(db);

        var request = new UlipCalculationRequest
        {
            PolicyNumber = "UL-TEST-PII",
            CustomerName = "Sensitive Name",
            PolicyholderName = "Policy Holder",
            DateOfBirth = new DateTime(1985, 6, 15),
            PolicyholderDateOfBirth = new DateTime(1980, 1, 1),
            ProductCode = "EWEALTH-ROYALE",
            EntryAge = 35,
            PolicyTerm = 20,
            Ppt = 10,
            AnnualizedPremium = 150000m,
            PremiumFrequency = "Yearly",
            FundAllocations = new List<UlipFundAllocation>
            {
                new() { FundType = "Equity Growth Fund", AllocationPercent = 70m },
                new() { FundType = "Debt Fund", AllocationPercent = 30m }
            }
        };

        var _ = await service.CalculateAsync(request);

        var log = await db.CalculationLogs.FirstAsync();
        StringAssert.DoesNotContain("DateOfBirth", log.InputJson);
        StringAssert.DoesNotContain("DateOfBirth", log.ResultJson);
        StringAssert.DoesNotContain("Policyholder", log.ResultJson);
        StringAssert.DoesNotContain("FundAllocations", log.ResultJson);
        StringAssert.DoesNotContain("Sensitive Name", log.ResultJson);

        // Result snapshot should still have Part A/B rows for reconstruction
        var snapshot = JsonSerializer.Deserialize<UlipCalculationService.UlipCalculationSnapshot>(log.ResultJson);
        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot!.PartARows, Is.Not.Empty);
        Assert.That(snapshot.PartBRows4, Is.Not.Empty);
        Assert.That(snapshot.PartBRows8, Is.Not.Empty);
    }
}
