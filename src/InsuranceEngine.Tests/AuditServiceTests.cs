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
public class AuditServiceTests
{
    private InsuranceDbContext _db = null!;
    private IAuditService _auditService = null!;
    private ICoreSystemGateway _gateway = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase("AuditTestDb_" + Guid.NewGuid())
            .Options;
        _db = new InsuranceDbContext(options);
        _db.Database.EnsureCreated();

        // Seed basic data for BenefitCalculationService
        SeedFactorData(_db);

        var calcService = new BenefitCalculationService(_db);

        var gatewayLogger = new Mock<ILogger<MockCoreSystemGateway>>();
        _gateway = new MockCoreSystemGateway(gatewayLogger.Object);

        var auditLogger = new Mock<ILogger<AuditService>>();
        _auditService = new AuditService(_db, _gateway, calcService, auditLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public async Task ProcessSinglePolicy_CreatesAuditCase()
    {
        var result = await _auditService.ProcessSinglePolicy("POL001", "PayoutVerification", "testuser");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.PolicyNumber, Is.EqualTo("POL001"));
        Assert.That(result.AuditType, Is.EqualTo("PayoutVerification"));
        Assert.That(result.Status, Is.EqualTo("Pending"));
        Assert.That(result.InputMode, Is.EqualTo("Single"));
        Assert.That(result.CoreSystemAmount, Is.GreaterThan(0));
        Assert.That(result.Id, Is.GreaterThan(0));

        // Verify saved to database
        var dbCase = await _db.AuditCases.FindAsync(result.Id);
        Assert.That(dbCase, Is.Not.Null);
        Assert.That(dbCase!.PolicyNumber, Is.EqualTo("POL001"));
    }

    [Test]
    public async Task ProcessSinglePolicy_AdditionBonus_CreatesAuditCase()
    {
        var result = await _auditService.ProcessSinglePolicy("POL002", "AdditionBonus", "testuser");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.PolicyNumber, Is.EqualTo("POL002"));
        Assert.That(result.AuditType, Is.EqualTo("AdditionBonus"));
        Assert.That(result.Status, Is.EqualTo("Pending"));
    }

    [Test]
    public async Task ApproveCase_UpdatesStatusAndCreatesDecision()
    {
        // Create a case first
        var caseResult = await _auditService.ProcessSinglePolicy("POL003", "PayoutVerification", "testuser");

        // Approve it
        var approved = await _auditService.ApproveCase(caseResult.Id, "Looks good", "approver1");

        Assert.That(approved.Status, Is.EqualTo("Approved"));
        Assert.That(approved.Remarks, Is.EqualTo("Looks good"));

        // Verify decision record
        var decisions = await _db.AuditDecisions.Where(d => d.AuditCaseId == caseResult.Id).ToListAsync();
        Assert.That(decisions, Has.Count.EqualTo(1));
        Assert.That(decisions[0].Decision, Is.EqualTo("Approved"));
        Assert.That(decisions[0].DecidedBy, Is.EqualTo("approver1"));
        Assert.That(decisions[0].PushedToCore, Is.True);
    }

    [Test]
    public async Task RejectCase_UpdatesStatusAndCreatesDecision()
    {
        var caseResult = await _auditService.ProcessSinglePolicy("POL004", "PayoutVerification", "testuser");

        var rejected = await _auditService.RejectCase(caseResult.Id, "Variance too high", "approver1");

        Assert.That(rejected.Status, Is.EqualTo("Rejected"));
        Assert.That(rejected.Remarks, Is.EqualTo("Variance too high"));

        var decisions = await _db.AuditDecisions.Where(d => d.AuditCaseId == caseResult.Id).ToListAsync();
        Assert.That(decisions, Has.Count.EqualTo(1));
        Assert.That(decisions[0].Decision, Is.EqualTo("Rejected"));
        Assert.That(decisions[0].PushedToCore, Is.False);
    }

    [Test]
    public async Task GetCases_ReturnsFilteredResults()
    {
        await _auditService.ProcessSinglePolicy("POL010", "PayoutVerification", "user1");
        await _auditService.ProcessSinglePolicy("POL011", "AdditionBonus", "user1");
        await _auditService.ProcessSinglePolicy("POL012", "PayoutVerification", "user1");

        // Filter by audit type
        var payoutResult = await _auditService.GetCases("PayoutVerification", null, null, 1, 50);
        Assert.That(payoutResult.Data.Count, Is.EqualTo(2));

        var bonusResult = await _auditService.GetCases("AdditionBonus", null, null, 1, 50);
        Assert.That(bonusResult.Data.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetCases_FiltersByStatus()
    {
        var case1 = await _auditService.ProcessSinglePolicy("POL020", "PayoutVerification", "user1");
        await _auditService.ProcessSinglePolicy("POL021", "PayoutVerification", "user1");
        await _auditService.ApproveCase(case1.Id, null, "approver");

        var pendingResult = await _auditService.GetCases(null, "Pending", null, 1, 50);
        Assert.That(pendingResult.Data.Count, Is.EqualTo(1));

        var approvedResult = await _auditService.GetCases(null, "Approved", null, 1, 50);
        Assert.That(approvedResult.Data.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetDashboard_ReturnsSummaryStats()
    {
        await _auditService.ProcessSinglePolicy("POL030", "PayoutVerification", "user1");
        await _auditService.ProcessSinglePolicy("POL031", "PayoutVerification", "user1");
        var case3 = await _auditService.ProcessSinglePolicy("POL032", "PayoutVerification", "user1");
        await _auditService.ApproveCase(case3.Id, null, "approver");

        var dashboard = await _auditService.GetDashboard();

        Assert.That(dashboard.TotalThisMonth, Is.GreaterThanOrEqualTo(3));
        Assert.That(dashboard.ApprovedCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(dashboard.PendingCount, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task GetBatchCases_ReturnsEmptyForNonExistentBatch()
    {
        var cases = await _auditService.GetBatchCases(999);
        Assert.That(cases, Is.Empty);
    }

    [Test]
    public async Task GetBatches_ReturnsBatchList()
    {
        // Create a batch directly
        _db.AuditBatches.Add(new AuditBatch
        {
            FileName = "test.csv",
            AuditType = "PayoutVerification",
            TotalCount = 5,
            ProcessedCount = 5,
            Status = "Processed",
            UploadedBy = "user1"
        });
        await _db.SaveChangesAsync();

        var batchResult = await _auditService.GetBatches(null, 1, 50);
        Assert.That(batchResult.Data.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(batchResult.Data[0].FileName, Is.EqualTo("test.csv"));
    }

    [Test]
    public void MockCoreSystemGateway_ReturnsConsistentData()
    {
        var policy1a = _gateway.FetchPolicyByPolicyNumber("POL100").Result;
        var policy1b = _gateway.FetchPolicyByPolicyNumber("POL100").Result;

        Assert.That(policy1a, Is.Not.Null);
        Assert.That(policy1b, Is.Not.Null);
        Assert.That(policy1a!.PolicyNumber, Is.EqualTo(policy1b!.PolicyNumber));
        Assert.That(policy1a.ProductName, Is.EqualTo(policy1b.ProductName));
    }

    [Test]
    public async Task MockCoreSystemGateway_FetchCoreAmount_ReturnsPositiveAmount()
    {
        var amount = await _gateway.FetchCoreSystemAmount("POL100", "PayoutVerification");
        Assert.That(amount.Amount, Is.GreaterThan(0));
    }

    [Test]
    public async Task MockCoreSystemGateway_PushApproval_ReturnsSuccess()
    {
        var response = await _gateway.PushApprovalToCore("POL100", "PayoutVerification", 500000, "OK", "user1");
        Assert.That(response.Success, Is.True);
        Assert.That(response.ReferenceNumber, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task AuditLogEntry_CreatedOnProcessing()
    {
        await _auditService.ProcessSinglePolicy("POL200", "PayoutVerification", "loguser");

        var logs = await _db.AuditLogEntries.Where(l => l.DoneBy == "loguser").ToListAsync();
        Assert.That(logs.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(logs.Any(l => l.EventType == "CaseCreated"), Is.True);
    }

    [Test]
    public async Task AuditLogEntry_CreatedOnApproval()
    {
        var caseResult = await _auditService.ProcessSinglePolicy("POL201", "PayoutVerification", "user1");
        await _auditService.ApproveCase(caseResult.Id, "Approved OK", "approver1");

        var logs = await _db.AuditLogEntries.Where(l => l.AuditCaseId == caseResult.Id).ToListAsync();
        Assert.That(logs.Any(l => l.EventType == "CaseApproved"), Is.True);
    }

    [Test]
    public async Task AuditLogEntry_CreatedOnRejection()
    {
        var caseResult = await _auditService.ProcessSinglePolicy("POL202", "PayoutVerification", "user1");
        await _auditService.RejectCase(caseResult.Id, "Not matching", "approver1");

        var logs = await _db.AuditLogEntries.Where(l => l.AuditCaseId == caseResult.Id).ToListAsync();
        Assert.That(logs.Any(l => l.EventType == "CaseRejected"), Is.True);
    }

    private void SeedFactorData(InsuranceDbContext db)
    {
        // Seed minimal GMB factor data for calculations
        db.GmbFactors.Add(new GmbFactor
        {
            Ppt = 10, Pt = 20, EntryAgeMin = 18, EntryAgeMax = 65,
            Option = "Immediate", Factor = 1100m
        });

        // Seed GSV factors
        for (int year = 1; year <= 20; year++)
        {
            db.GsvFactors.Add(new GsvFactor { Ppt = 10, Pt = 20, PolicyYear = year, FactorPercent = 30m + year });
        }

        // Seed SSV factors
        for (int year = 1; year <= 20; year++)
        {
            db.SsvFactors.Add(new SsvFactor
            {
                Ppt = 10, Pt = 20, Option = "Immediate", PolicyYear = year,
                Factor1 = 40m + year, Factor2 = 50m + year
            });
        }

        // Seed Loyalty factors
        db.LoyaltyFactors.Add(new LoyaltyFactor { Ppt = 10, PolicyYearFrom = 1, PolicyYearTo = 10, RatePercent = 0m });
        db.LoyaltyFactors.Add(new LoyaltyFactor { Ppt = 10, PolicyYearFrom = 11, PolicyYearTo = 20, RatePercent = 0.5m });

        db.SaveChanges();
    }
}
