using InsuranceEngine.Api;
using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace InsuranceEngine.Tests;

[TestFixture]
public class CalculationIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the real DB context
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<InsuranceDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    // Use in-memory database with a fixed name so all scopes share it
                    var dbName = "TestDb_" + System.Guid.NewGuid().ToString();
                    services.AddDbContext<InsuranceDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));
                });
            });

        _client = _factory.CreateClient();

        // Seed test data
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InsuranceDbContext>();
        db.Database.EnsureCreated();
        SeedTestData(db);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private void SeedTestData(InsuranceDbContext db)
    {
        var insurer = new Insurer { Name = "Test Insurer", Code = "TI" };
        db.Insurers.Add(insurer);
        db.SaveChanges();

        var product = new Product { InsurerId = insurer.Id, Name = "Test Product", Code = "CENTURY_INCOME", ProductType = "Traditional" };
        db.Products.Add(product);
        db.SaveChanges();

        var version = new ProductVersion { ProductId = product.Id, Version = "1.0", IsActive = true, EffectiveDate = System.DateTime.UtcNow };
        db.ProductVersions.Add(version);
        db.SaveChanges();

        db.ProductFormulas.AddRange(
            new ProductFormula { ProductVersionId = version.Id, Name = "GMB", Expression = "AP * 11.5", ExecutionOrder = 1 },
            new ProductFormula { ProductVersionId = version.Id, Name = "GSV", Expression = "GMB * 0.30", ExecutionOrder = 2 },
            new ProductFormula { ProductVersionId = version.Id, Name = "SSV", Expression = "AP * 12", ExecutionOrder = 3 },
            new ProductFormula { ProductVersionId = version.Id, Name = "MATURITY_BENEFIT", Expression = "GMB", ExecutionOrder = 4 },
            new ProductFormula { ProductVersionId = version.Id, Name = "DEATH_BENEFIT", Expression = "MAX(10*AP, 1.05*TotalPremiumPaid, SurrenderValue)", ExecutionOrder = 5 }
        );
        db.SaveChanges();
    }

    [Test]
    public async Task TraditionalCalculation_ReturnsCorrectResults()
    {
        var request = new TraditionalCalculationRequest
        {
            ProductCode = "CENTURY_INCOME",
            Parameters = new Dictionary<string, decimal>
            {
                { "AP", 10000m }, { "SA", 100000m }, { "PPT", 10m }, { "PT", 20m },
                { "Age", 35m }, { "TotalPremiumPaid", 50000m }, { "SurrenderValue", 40000m }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/calculation/traditional", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TraditionalCalculationResponse>();
        Assert.IsNotNull(result);
        Assert.AreEqual("CENTURY_INCOME", result!.ProductCode);
        Assert.AreEqual(115000m, result.Results["GMB"]);
        Assert.AreEqual(34500m, result.Results["GSV"]);
        Assert.AreEqual(120000m, result.Results["SSV"]);
        Assert.AreEqual(115000m, result.Results["MATURITY_BENEFIT"]);
        Assert.AreEqual(100000m, result.Results["DEATH_BENEFIT"]);
    }

    [Test]
    public async Task TraditionalCalculation_UnknownProduct_Returns404()
    {
        var request = new TraditionalCalculationRequest
        {
            ProductCode = "UNKNOWN_PRODUCT",
            Parameters = new Dictionary<string, decimal> { { "AP", 10000m } }
        };

        var response = await _client.PostAsJsonAsync("/api/calculation/traditional", request);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
