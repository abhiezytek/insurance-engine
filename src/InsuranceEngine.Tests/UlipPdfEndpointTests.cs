using InsuranceEngine.Api;
using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace InsuranceEngine.Tests;

[TestFixture]
public class UlipPdfEndpointTests
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
                    // Replace real DB with in-memory for tests
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<InsuranceDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    var dbName = "UlipPdfTestDb_" + Guid.NewGuid();
                    services.AddDbContext<InsuranceDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));
                });
            });

        _client = _factory.CreateClient();

        // Ensure database exists
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InsuranceDbContext>();
        db.Database.EnsureCreated();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task PdfEndpoint_Should_Return_Html_With_Business_Labels()
    {
        var policyNumber = "UL-PDF-TEST-001";
        var calculateRequest = new UlipCalculationRequest
        {
            PolicyNumber = policyNumber,
            CustomerName = "Test Customer",
            ProductCode = "EWEALTH-ROYALE",
            Gender = "Male",
            EntryAge = 35,
            PolicyTerm = 15,
            Ppt = 10,
            TypeOfPpt = "Limited",
            AnnualizedPremium = 100000,
            SumAssured = 1000000,
            PremiumFrequency = "Yearly",
            RiskPreference = "Self-Managed",
            FundAllocations =
            [
                new() { FundType = "Blue-chip Equity Fund", AllocationPercent = 100 }
            ]
        };

        // Calculate and persist illustration
        var calcResponse = await _client.PostAsJsonAsync("/api/ulip/calculate", calculateRequest);
        Assert.That(calcResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Calculation endpoint should succeed.");

        // Download the HTML (PDF) view
        var pdfResponse = await _client.GetAsync($"/api/ulip/pdf/{policyNumber}");
        Assert.That(pdfResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "PDF endpoint should succeed after calculation.");
        Assert.That(pdfResponse.Content.Headers.ContentType?.MediaType, Does.StartWith("text/html"));

        var html = await pdfResponse.Content.ReadAsStringAsync();
        var normalizedHtml = html.Replace("<br/>", " ").Replace("<br />", " ");

        // Required labels
        string[] expectedLabels = new[]
        {
            "Annualized Premium",
            "Premium Installment",
            "Part A",
            "Part B",
            "Mortality Charges",
            "Additional Risk Benefit Charges",
            "Fund at End of Year",
            "Surrender Value",
            "Death Benefit",
            "Policy Administration Charges",
            "Fund Management Charges",
            "Loyalty Addition",
            "Wealth Booster"
        };

        foreach (var label in expectedLabels)
        {
            StringAssert.Contains(label, normalizedHtml, $"Expected label '{label}' missing in HTML output.");
        }
    }

}
