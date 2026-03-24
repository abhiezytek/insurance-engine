using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace InsuranceEngine.Tests;

[TestFixture]
public class PayoutServiceTests
{
    private InsuranceDbContext _db = null!;
    private IPayoutService _payoutService = null!;
    private ICoreSystemGateway _gateway = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase("PayoutTestDb_" + Guid.NewGuid())
            .Options;
        _db = new InsuranceDbContext(options);
        _db.Database.EnsureCreated();

        SeedFactorData(_db);

        var calcService = new BenefitCalculationService(_db);

        var gatewayLogger = new Mock<ILogger<MockCoreSystemGateway>>();
        _gateway = new MockCoreSystemGateway(gatewayLogger.Object);

        var payoutLogger = new Mock<ILogger<PayoutService>>();
        _payoutService = new PayoutService(_db, _gateway, calcService, payoutLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    // ─── SearchAndVerify ─────────────────────────────────────────────────────

    [Test]
    public async Task SearchAndVerify_CreatesPayoutCase()
    {
        var result = await _payoutService.SearchAndVerify("POL001", "Maturity", "testuser");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.PolicyNumber, Is.EqualTo("POL001"));
        Assert.That(result.PayoutType, Is.EqualTo("Maturity"));
        Assert.That(result.Status, Is.EqualTo("Pending"));
        Assert.That(result.InputMode, Is.EqualTo("Single"));
        Assert.That(result.CoreSystemAmount, Is.GreaterThan(0));
        Assert.That(result.Id, Is.GreaterThan(0));

        var dbCase = await _db.PayoutCases.FindAsync(result.Id);
        Assert.That(dbCase, Is.Not.Null);
        Assert.That(dbCase!.PolicyNumber, Is.EqualTo("POL001"));
    }

    [Test]
    public async Task SearchAndVerify_CreatesWorkflowHistory()
    {
        var result = await _payoutService.SearchAndVerify("POL002", "Maturity", "testuser");

        var history = await _db.PayoutWorkflowHistories
            .Where(h => h.PayoutCaseId == result.Id)
            .ToListAsync();

        Assert.That(history, Has.Count.EqualTo(1));
        Assert.That(history[0].Action, Is.EqualTo("Created"));
        Assert.That(history[0].ToStatus, Is.EqualTo("Pending"));
    }

    // ─── 2-Level Approval: Checker ───────────────────────────────────────────

    [Test]
    public async Task CheckerApprove_UpdatesStatusToCheckerApproved()
    {
        var caseResult = await _payoutService.SearchAndVerify("POL003", "Maturity", "user1");

        var approved = await _payoutService.CheckerApprove(caseResult.Id, "Looks correct", "checker1");

        Assert.That(approved.Status, Is.EqualTo("CheckerApproved"));
        Assert.That(approved.Remarks, Is.EqualTo("Looks correct"));

        var history = await _db.PayoutWorkflowHistories
            .Where(h => h.PayoutCaseId == caseResult.Id)
            .OrderBy(h => h.PerformedAt)
            .ToListAsync();

        Assert.That(history, Has.Count.EqualTo(2));
        Assert.That(history[1].Action, Is.EqualTo("CheckerApproved"));
        Assert.That(history[1].PerformedBy, Is.EqualTo("checker1"));
    }

    [Test]
    public async Task CheckerReject_UpdatesStatusToCheckerRejected()
    {
        var caseResult = await _payoutService.SearchAndVerify("POL004", "Maturity", "user1");

        var rejected = await _payoutService.CheckerReject(caseResult.Id, "Variance too high", "checker1");

        Assert.That(rejected.Status, Is.EqualTo("CheckerRejected"));
    }

    [Test]
    public void CheckerApprove_ThrowsIfNotPending()
    {
        var caseResult = _payoutService.SearchAndVerify("POL005", "Maturity", "user1").Result;
        _payoutService.CheckerApprove(caseResult.Id, null, "checker1").Wait();

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _payoutService.CheckerApprove(caseResult.Id, null, "checker2"));
    }

    // ─── 2-Level Approval: Authorizer ────────────────────────────────────────

    [Test]
    public async Task AuthorizerApprove_UpdatesStatusToAuthorized()
    {
        var caseResult = await _payoutService.SearchAndVerify("POL006", "Maturity", "user1");
        await _payoutService.CheckerApprove(caseResult.Id, null, "checker1");

        var authorized = await _payoutService.AuthorizerApprove(caseResult.Id, "Final approval", "authorizer1");

        Assert.That(authorized.Status, Is.EqualTo("Authorized"));

        var history = await _db.PayoutWorkflowHistories
            .Where(h => h.PayoutCaseId == caseResult.Id)
            .OrderBy(h => h.PerformedAt)
            .ToListAsync();

        Assert.That(history, Has.Count.EqualTo(3));
        Assert.That(history[2].Action, Is.EqualTo("Authorized"));
        Assert.That(history[2].PushStatus, Is.EqualTo("Success"));
        Assert.That(history[2].PushReferenceNumber, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task AuthorizerReject_UpdatesStatusToRejected()
    {
        var caseResult = await _payoutService.SearchAndVerify("POL007", "Maturity", "user1");
        await _payoutService.CheckerApprove(caseResult.Id, null, "checker1");

        var rejected = await _payoutService.AuthorizerReject(caseResult.Id, "Policy issue", "authorizer1");

        Assert.That(rejected.Status, Is.EqualTo("Rejected"));
    }

    [Test]
    public void AuthorizerApprove_ThrowsIfNotCheckerApproved()
    {
        var caseResult = _payoutService.SearchAndVerify("POL008", "Maturity", "user1").Result;

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _payoutService.AuthorizerApprove(caseResult.Id, null, "authorizer1"));
    }

    // ─── Full 2-level workflow ───────────────────────────────────────────────

    [Test]
    public async Task FullWorkflow_Pending_CheckerApproved_Authorized()
    {
        var created = await _payoutService.SearchAndVerify("POL010", "Maturity", "maker");
        Assert.That(created.Status, Is.EqualTo("Pending"));

        var checked_ = await _payoutService.CheckerApprove(created.Id, "OK at level 1", "checker");
        Assert.That(checked_.Status, Is.EqualTo("CheckerApproved"));

        var authorized = await _payoutService.AuthorizerApprove(created.Id, "OK at level 2", "authorizer");
        Assert.That(authorized.Status, Is.EqualTo("Authorized"));

        var history = await _db.PayoutWorkflowHistories
            .Where(h => h.PayoutCaseId == created.Id)
            .OrderBy(h => h.PerformedAt)
            .ToListAsync();

        Assert.That(history, Has.Count.EqualTo(3));
        Assert.That(history.Select(h => h.Action).ToList(),
            Is.EqualTo(new[] { "Created", "CheckerApproved", "Authorized" }));
    }

    // ─── Queries ─────────────────────────────────────────────────────────────

    [Test]
    public async Task GetCases_ReturnsFilteredByStatus()
    {
        var case1 = await _payoutService.SearchAndVerify("POL020", "Maturity", "user1");
        await _payoutService.SearchAndVerify("POL021", "Maturity", "user1");
        await _payoutService.CheckerApprove(case1.Id, null, "checker");

        var pending = await _payoutService.GetCases(null, "Pending", null, 1, 50);
        Assert.That(pending.Count, Is.EqualTo(1));

        var checkerApproved = await _payoutService.GetCases(null, "CheckerApproved", null, 1, 50);
        Assert.That(checkerApproved.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetDashboard_ReturnsSummaryStats()
    {
        await _payoutService.SearchAndVerify("POL030", "Maturity", "user1");
        await _payoutService.SearchAndVerify("POL031", "Maturity", "user1");
        var case3 = await _payoutService.SearchAndVerify("POL032", "Maturity", "user1");
        await _payoutService.CheckerApprove(case3.Id, null, "checker");

        var dashboard = await _payoutService.GetDashboard();

        Assert.That(dashboard.TotalThisMonth, Is.GreaterThanOrEqualTo(3));
        Assert.That(dashboard.PendingCount, Is.GreaterThanOrEqualTo(2));
        Assert.That(dashboard.CheckerApprovedCount, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task GetBatchCases_ReturnsEmptyForNonExistentBatch()
    {
        var cases = await _payoutService.GetBatchCases(999);
        Assert.That(cases, Is.Empty);
    }

    [Test]
    public async Task GetBatches_ReturnsBatchList()
    {
        _db.PayoutBatches.Add(new PayoutBatch
        {
            BatchType = "SystemGenerated",
            PayoutType = "Maturity",
            TotalCount = 5,
            ProcessedCount = 5,
            Status = "Processed",
            CreatedBy = "user1"
        });
        await _db.SaveChangesAsync();

        var batches = await _payoutService.GetBatches(null, 1, 50);
        Assert.That(batches.Count, Is.GreaterThanOrEqualTo(1));
    }

    // ─── Batch Generation ────────────────────────────────────────────────────

    [Test]
    public async Task GenerateBatch_CreatesSystemGeneratedBatch()
    {
        var batch = await _payoutService.GenerateBatch("Maturity", null, null, 3, "batchuser");

        Assert.That(batch, Is.Not.Null);
        Assert.That(batch.BatchType, Is.EqualTo("SystemGenerated"));
        Assert.That(batch.TotalCount, Is.EqualTo(3));
        Assert.That(batch.ProcessedCount, Is.EqualTo(3));
        Assert.That(batch.Status, Is.EqualTo("Processed"));

        var cases = await _payoutService.GetBatchCases(batch.Id);
        Assert.That(cases.Count, Is.EqualTo(3));
        Assert.That(cases.All(c => c.InputMode == "SystemGenerated"), Is.True);
    }

    // ─── File Upload ─────────────────────────────────────────────────────────

    [Test]
    public async Task ProcessUploadedFile_ProcessesCsvPolicies()
    {
        var csv = "PolicyNumber\nPOL100\nPOL101\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        var batch = await _payoutService.ProcessUploadedFile(stream, "test.csv", "Maturity", "uploader");

        Assert.That(batch, Is.Not.Null);
        Assert.That(batch.BatchType, Is.EqualTo("FileUpload"));
        Assert.That(batch.FileName, Is.EqualTo("test.csv"));
        Assert.That(batch.TotalCount, Is.EqualTo(2));
        Assert.That(batch.ProcessedCount, Is.EqualTo(2));

        var files = await _db.PayoutFiles.Where(f => f.BatchId == batch.Id).ToListAsync();
        Assert.That(files.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(files[0].FileType, Is.EqualTo("Upload"));
    }

    // ─── Export ──────────────────────────────────────────────────────────────

    [Test]
    public async Task ExportCases_CsvFormat_ReturnsValidCsv()
    {
        await _payoutService.SearchAndVerify("POLEXP1", "Maturity", "user1");

        var (content, fileName, contentType) = await _payoutService.ExportCases(null, "CSV", "exporter");

        Assert.That(contentType, Is.EqualTo("text/csv"));
        Assert.That(fileName, Does.EndWith(".csv"));

        var csvText = System.Text.Encoding.UTF8.GetString(content);
        Assert.That(csvText, Does.Contain("PolicyNumber"));
        Assert.That(csvText, Does.Contain("POLEXP1"));

        var exportFiles = await _db.PayoutFiles.Where(f => f.FileType == "Export").ToListAsync();
        Assert.That(exportFiles.Count, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task ExportCases_JsonFormat_ReturnsValidJson()
    {
        await _payoutService.SearchAndVerify("POLEXP2", "Maturity", "user1");

        var (content, fileName, contentType) = await _payoutService.ExportCases(null, "JSON", "exporter");

        Assert.That(contentType, Is.EqualTo("application/json"));
        Assert.That(fileName, Does.EndWith(".json"));

        var jsonText = System.Text.Encoding.UTF8.GetString(content);
        Assert.That(jsonText, Does.Contain("POLEXP2"));
    }

    // ─── Maker-Checker Separation (GAP 1.3) ────────────────────────────────────

    [Test]
    public void CheckerApprove_ThrowsIfSameUserAsMaker()
    {
        var caseResult = _payoutService.SearchAndVerify("POLMC01", "Maturity", "maker1").Result;

        // maker1 submitted → maker1 tries to approve → blocked
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _payoutService.CheckerApprove(caseResult.Id, null, "maker1"));
    }

    [Test]
    public void CheckerReject_ThrowsIfSameUserAsMaker()
    {
        var caseResult = _payoutService.SearchAndVerify("POLMC02", "Maturity", "maker1").Result;

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _payoutService.CheckerReject(caseResult.Id, null, "maker1"));
    }

    [Test]
    public void AuthorizerApprove_ThrowsIfSameUserAsChecker()
    {
        var caseResult = _payoutService.SearchAndVerify("POLMC03", "Maturity", "maker1").Result;
        _payoutService.CheckerApprove(caseResult.Id, null, "checker1").Wait();

        // checker1 approved at L1 → checker1 tries to authorize → blocked
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _payoutService.AuthorizerApprove(caseResult.Id, null, "checker1"));
    }

    [Test]
    public void AuthorizerReject_ThrowsIfSameUserAsChecker()
    {
        var caseResult = _payoutService.SearchAndVerify("POLMC04", "Maturity", "maker1").Result;
        _payoutService.CheckerApprove(caseResult.Id, null, "checker1").Wait();

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _payoutService.AuthorizerReject(caseResult.Id, null, "checker1"));
    }

    [Test]
    public async Task MakerCheckerAuthorizer_ThreeDistinctUsers_Succeeds()
    {
        var created = await _payoutService.SearchAndVerify("POLMC05", "Maturity", "maker1");
        var checked_ = await _payoutService.CheckerApprove(created.Id, "OK", "checker1");
        var authorized = await _payoutService.AuthorizerApprove(created.Id, "Final OK", "authorizer1");

        Assert.That(authorized.Status, Is.EqualTo("Authorized"));
    }

    // ─── File Integrity (GAP 1.5) ────────────────────────────────────────────

    [Test]
    public async Task ExportCases_CsvFormat_StoresFileHash()
    {
        await _payoutService.SearchAndVerify("POLHASH1", "Maturity", "user1");

        var (content, fileName, _) = await _payoutService.ExportCases(null, "CSV", "exporter");

        var exportFiles = await _db.PayoutFiles.Where(f => f.FileType == "Export").ToListAsync();
        Assert.That(exportFiles.Count, Is.GreaterThanOrEqualTo(1));

        var file = exportFiles.Last();
        Assert.That(file.FileHash, Is.Not.Null.And.Not.Empty);
        Assert.That(file.FileHash!.Length, Is.EqualTo(64)); // SHA256 hex = 64 chars

        // Verify hash matches content
        var expectedHash = System.Security.Cryptography.SHA256.HashData(content);
        var expectedHex = Convert.ToHexString(expectedHash).ToLowerInvariant();
        Assert.That(file.FileHash, Is.EqualTo(expectedHex));
    }

    [Test]
    public async Task ExportCases_JsonFormat_StoresFileHash()
    {
        await _payoutService.SearchAndVerify("POLHASH2", "Maturity", "user1");

        var (content, _, _) = await _payoutService.ExportCases(null, "JSON", "exporter");

        var exportFiles = await _db.PayoutFiles.Where(f => f.FileType == "Export" && f.FileFormat == "JSON").ToListAsync();
        Assert.That(exportFiles.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(exportFiles.Last().FileHash, Is.Not.Null.And.Not.Empty);
    }

    // ─── Seed ────────────────────────────────────────────────────────────────

    private void SeedFactorData(InsuranceDbContext db)
    {
        db.GmbFactors.Add(new GmbFactor
        {
            Ppt = 10, Pt = 20, EntryAgeMin = 18, EntryAgeMax = 65,
            Option = "Immediate", Factor = 1100m
        });

        for (int year = 1; year <= 20; year++)
        {
            db.GsvFactors.Add(new GsvFactor { Ppt = 10, Pt = 20, PolicyYear = year, FactorPercent = 30m + year });
        }

        for (int year = 1; year <= 20; year++)
        {
            db.SsvFactors.Add(new SsvFactor
            {
                Ppt = 10, Pt = 20, Option = "Immediate", PolicyYear = year,
                Factor1 = 40m + year, Factor2 = 50m + year
            });
        }

        db.LoyaltyFactors.Add(new LoyaltyFactor { Ppt = 10, PolicyYearFrom = 1, PolicyYearTo = 10, RatePercent = 0m });
        db.LoyaltyFactors.Add(new LoyaltyFactor { Ppt = 10, PolicyYearFrom = 11, PolicyYearTo = 20, RatePercent = 0.5m });

        db.SaveChanges();
    }
}
