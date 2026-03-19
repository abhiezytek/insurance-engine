using InsuranceEngine.Api;
using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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
        var html = await GetPdfHtmlAsync("UL-PDF-TEST-001");
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

    [Test]
    public async Task PdfEndpoint_Should_Have_Sections_In_Order()
    {
        var html = await GetPdfHtmlAsync("UL-PDF-ORDER-001");

        var idxSummary = html.IndexOf("Annualized Premium", StringComparison.OrdinalIgnoreCase);
        var idxPartA = html.IndexOf("Part A", StringComparison.OrdinalIgnoreCase);
        var idxPartB = html.IndexOf("Part B", StringComparison.OrdinalIgnoreCase);

        Assert.That(idxSummary, Is.GreaterThanOrEqualTo(0), "Summary block should be present.");
        Assert.That(idxPartA, Is.GreaterThan(idxSummary), "Part A should appear after summary.");
        Assert.That(idxPartB, Is.GreaterThan(idxPartA), "Part B should appear after Part A.");
    }

    [Test]
    public async Task PdfEndpoint_PartA_Should_Have_Headers_In_Expected_Order()
    {
        var html = await GetPdfHtmlAsync("UL-PDF-PARTA-001");
        var partA = ExtractSection(html, "Part A", "Part B");

        AssertContainsInOrder(
            partA,
            "Policy Year",
            "Annualized Premium",
            "Mortality Charges",
            "Additional Risk Benefit Charges",
            "Other Charges",
            "GST",
            "Fund at End of Year",
            "Surrender Value",
            "Death Benefit");
    }

    [Test]
    public async Task PdfEndpoint_PartB_Should_Have_Headers_In_Expected_Order()
    {
        var html = await GetPdfHtmlAsync("UL-PDF-PARTB-001");
        var partB = ExtractSection(html, "Part B", "Legend");

        AssertContainsInOrder(
            partB,
            "Policy Year",
            "Annualized Premium",
            "Premium Allocation Charges",
            "Annualized Premium less Premium Allocation Charges",
            "Mortality Charges",
            "Additional Risk Benefit Charges",
            "GST",
            "Policy Administration Charges",
            "Extra Premium Allocation",
            "Fund Before Fund Management Charges",
            "Fund Management Charges",
            "Loyalty Addition",
            "Wealth Booster",
            "Return of Charges (combined)",
            "Fund at End of Year",
            "Surrender Value",
            "Death Benefit");
    }

    [Test]
    public async Task PdfEndpoint_Should_Render_Numeric_Content()
    {
        var html = await GetPdfHtmlAsync("UL-PDF-NUMERIC-001");
        var numericPattern = new Regex(@"<td[^>]*>[^<]*\d[\d,]*\.?\d*[^<]*</td>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        Assert.That(numericPattern.IsMatch(html), Is.True, "HTML should contain at least one numeric table cell.");
    }

    private async Task<string> GetPdfHtmlAsync(string policyNumber)
    {
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

        var calcResponse = await _client.PostAsJsonAsync("/api/ulip/calculate", calculateRequest);
        Assert.That(calcResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Calculation endpoint should succeed.");

        var pdfResponse = await _client.GetAsync($"/api/ulip/pdf/{policyNumber}");
        Assert.That(pdfResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "PDF endpoint should succeed after calculation.");
        Assert.That(pdfResponse.Content.Headers.ContentType?.MediaType, Does.StartWith("text/html"));

        var html = await pdfResponse.Content.ReadAsStringAsync();
        Assert.That(string.IsNullOrWhiteSpace(html), Is.False, "HTML response should not be empty.");
        return html;
    }

    private static void AssertContainsInOrder(string text, params string[] tokens)
    {
        var normalized = NormalizeText(text);
        var current = -1;
        foreach (var token in tokens)
        {
            var idx = normalized.IndexOf(token, current + 1, StringComparison.OrdinalIgnoreCase);
            Assert.That(idx, Is.GreaterThan(current), $"Expected '{token}' after previous token in order.");
            current = idx;
        }
    }

    private static string ExtractSection(string html, string sectionName, string? nextSectionName)
    {
        var start = html.IndexOf(sectionName, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return string.Empty;

        var end = nextSectionName == null
            ? html.Length
            : html.IndexOf(nextSectionName, start + sectionName.Length, StringComparison.OrdinalIgnoreCase);

        if (end < 0) end = html.Length;
        var segment = html.Substring(start, end - start);
        return NormalizeText(segment);
    }

    private static string NormalizeText(string text)
    {
        var noBreaks = Regex.Replace(text, "<br\\s*/?>", " ", RegexOptions.IgnoreCase);
        var noTags = Regex.Replace(noBreaks, "<[^>]+>", " ");
        var decoded = WebUtility.HtmlDecode(noTags);
        return Regex.Replace(decoded, "\\s+", " ").Trim();
    }

}
