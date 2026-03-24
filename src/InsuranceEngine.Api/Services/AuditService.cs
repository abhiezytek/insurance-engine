using System.Data;
using System.Text;
using Dapper;
using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Services;

public class AuditService : IAuditService
{
    private readonly InsuranceDbContext _db;
    private readonly ICoreSystemGateway _gateway;
    private readonly IBenefitCalculationService _calcService;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        InsuranceDbContext db,
        ICoreSystemGateway gateway,
        IBenefitCalculationService calcService,
        ILogger<AuditService> logger)
    {
        _db = db;
        _gateway = gateway;
        _calcService = calcService;
        _logger = logger;
    }

    // ─── Dapper connection helper ──────────────────────────────────────────────

    private IDbConnection? TryGetDbConnection()
    {
        try { return _db.Database.GetDbConnection(); }
        catch { return null; }
    }

    public async Task<AuditCaseResultDto> ProcessSinglePolicy(string policyNumber, string auditType, string userId)
    {
        _logger.LogInformation("Processing single policy {PolicyNumber} for audit type {AuditType}", policyNumber, auditType);

        var policy = await _gateway.FetchPolicyByPolicyNumber(policyNumber)
            ?? throw new InvalidOperationException($"Policy {policyNumber} not found in core system.");

        var coreResult = await _gateway.FetchCoreSystemAmount(policyNumber, auditType);

        var biRequest = new BenefitIllustrationRequest
        {
            AnnualPremium = policy.AnnualPremium,
            Ppt = policy.PremiumPayingTerm,
            PolicyTerm = policy.PolicyTerm,
            EntryAge = policy.EntryAge,
            Option = policy.Option,
            Channel = policy.Channel,
            PremiumsPaid = policy.PremiumsPaid,
            IsPreIssuance = false,
            RiskCommencementDate = policy.PolicyAnniversary
        };

        var biResponse = await _calcService.CalculateAsync(biRequest);

        decimal precisionProAmount = auditType == "PayoutVerification"
            ? biResponse.GuaranteedMaturityBenefit
            : biResponse.YearlyTable.LastOrDefault()?.LoyaltyIncome ?? 0m;

        decimal variance = Math.Round(
            coreResult.Amount - precisionProAmount,
            2,
            MidpointRounding.AwayFromZero);

        var auditCase = new AuditCase
        {
            PolicyNumber = policyNumber,
            ProductName = policy.ProductName,
            Uin = policy.Uin,
            AuditType = auditType,
            InputMode = "Single",
            CoreSystemAmount = coreResult.Amount,
            PrecisionProAmount = precisionProAmount,
            Variance = variance,
            Status = "Pending",
            PolicyAnniversary = policy.PolicyAnniversary,
            ProductVersion = biResponse.ProductVersion ?? "v-default",
            FactorVersion = biResponse.FactorVersion ?? "table-default",
            FormulaVersion = biResponse.FormulaVersion ?? "v-default",
            CalculationSource = "PrecisionPro",
            CalculatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.AuditCases.Add(auditCase);
        await _db.SaveChangesAsync();

        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            AuditCaseId = auditCase.Id,
            EventType = "CaseCreated",
            NewValue = $"Policy={MaskPolicy(policyNumber)}, AuditType={auditType}, Variance={variance}",
            DoneBy = userId,
            DoneAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("Audit case {CaseId} created for {PolicyNumber} with variance {Variance}", auditCase.Id, policyNumber, variance);

        return MapToDto(auditCase);
    }

    public async Task<AuditCaseResultDto> ApproveCase(int caseId, string? remarks, string userId)
    {
        _logger.LogInformation("Approving case {CaseId} by {UserId}", caseId, userId);

        var auditCase = await _db.AuditCases.FindAsync(caseId)
            ?? throw new InvalidOperationException($"Audit case {caseId} not found.");

        var oldStatus = auditCase.Status;
        auditCase.Status = "Approved";
        auditCase.Remarks = remarks;

        var pushResult = await _gateway.PushApprovalToCore(
            auditCase.PolicyNumber,
            auditCase.AuditType,
            auditCase.PrecisionProAmount,
            remarks,
            userId);

        var decision = new AuditDecision
        {
            AuditCaseId = caseId,
            Decision = "Approved",
            Remarks = remarks,
            DecidedBy = userId,
            DecidedAt = DateTime.UtcNow,
            PushedToCore = pushResult.Success,
            PushStatus = pushResult.Success ? "Success" : "Failed",
            PushTimestamp = DateTime.UtcNow,
            PushErrorMessage = pushResult.ErrorMessage
        };

        _db.AuditDecisions.Add(decision);

        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            AuditCaseId = caseId,
            EventType = "CaseApproved",
            OldValue = oldStatus,
            NewValue = $"Approved (Ref: {pushResult.ReferenceNumber})",
            DoneBy = userId,
            DoneAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation("Case {CaseId} approved, push ref={RefNumber}", caseId, pushResult.ReferenceNumber);

        return MapToDto(auditCase);
    }

    public async Task<AuditCaseResultDto> RejectCase(int caseId, string? remarks, string userId)
    {
        _logger.LogInformation("Rejecting case {CaseId} by {UserId}", caseId, userId);

        var auditCase = await _db.AuditCases.FindAsync(caseId)
            ?? throw new InvalidOperationException($"Audit case {caseId} not found.");

        var oldStatus = auditCase.Status;
        auditCase.Status = "Rejected";
        auditCase.Remarks = remarks;

        var decision = new AuditDecision
        {
            AuditCaseId = caseId,
            Decision = "Rejected",
            Remarks = remarks,
            DecidedBy = userId,
            DecidedAt = DateTime.UtcNow,
            PushedToCore = false,
            PushStatus = null,
            PushTimestamp = null,
            PushErrorMessage = null
        };

        _db.AuditDecisions.Add(decision);

        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            AuditCaseId = caseId,
            EventType = "CaseRejected",
            OldValue = oldStatus,
            NewValue = "Rejected",
            DoneBy = userId,
            DoneAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation("Case {CaseId} rejected", caseId);

        return MapToDto(auditCase);
    }

    // ─── Queries (using Dapper for read-heavy paths with EF Core fallback) ────

    public async Task<(List<AuditCaseResultDto> Data, int TotalCount)> GetCases(string? auditType, string? status, string? inputMode, int page, int pageSize)
    {
        try
        {
            return await GetCasesDapper(auditType, status, inputMode, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Dapper audit cases query failed, falling back to EF Core");
            return await GetCasesFallback(auditType, status, inputMode, page, pageSize);
        }
    }

    private async Task<(List<AuditCaseResultDto> Data, int TotalCount)> GetCasesDapper(string? auditType, string? status, string? inputMode, int page, int pageSize)
    {
        var conn = TryGetDbConnection();
        if (conn == null) return await GetCasesFallback(auditType, status, inputMode, page, pageSize);

        var whereClauses = new StringBuilder();
        var countSql = new StringBuilder("SELECT COUNT(*) FROM AuditCases c WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(auditType))
        {
            whereClauses.Append(" AND c.AuditType = @AuditType");
            parameters.Add("AuditType", auditType);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            whereClauses.Append(" AND c.Status = @Status");
            parameters.Add("Status", status);
        }
        if (!string.IsNullOrWhiteSpace(inputMode))
        {
            whereClauses.Append(" AND c.InputMode = @InputMode");
            parameters.Add("InputMode", inputMode);
        }

        countSql.Append(whereClauses);
        var totalCount = await conn.ExecuteScalarAsync<int>(countSql.ToString(), parameters);

        var sql = $@"
            SELECT * FROM AuditCases c WHERE 1=1{whereClauses}
            ORDER BY c.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var cases = await conn.QueryAsync<AuditCase>(sql, parameters);
        return (cases.Select(MapToDto).ToList(), totalCount);
    }

    private async Task<(List<AuditCaseResultDto> Data, int TotalCount)> GetCasesFallback(string? auditType, string? status, string? inputMode, int page, int pageSize)
    {
        var query = _db.AuditCases.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(auditType))
            query = query.Where(c => c.AuditType == auditType);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(inputMode))
            query = query.Where(c => c.InputMode == inputMode);

        var totalCount = await query.CountAsync();
        var cases = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (cases.Select(MapToDto).ToList(), totalCount);
    }

    public async Task<AuditDashboardDto> GetDashboard()
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var conn = TryGetDbConnection();
        if (conn != null)
        {
            try
            {
                const string sql = @"
                    SELECT
                        COUNT(*)                                                  AS TotalThisMonth,
                        SUM(CASE WHEN Status = 'Approved' THEN 1 ELSE 0 END)     AS ApprovedCount,
                        SUM(CASE WHEN Status = 'Rejected' THEN 1 ELSE 0 END)     AS RejectedCount,
                        SUM(CASE WHEN Status = 'Pending'  THEN 1 ELSE 0 END)     AS PendingCount,
                        ISNULL(SUM(ABS(Variance)), 0)                             AS TotalVariance
                    FROM AuditCases
                    WHERE CreatedAt >= @StartOfMonth";

                var result = await conn.QueryFirstOrDefaultAsync<AuditDashboardDto>(sql, new { StartOfMonth = startOfMonth });
                return result ?? new AuditDashboardDto();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Dapper audit dashboard query failed, falling back to EF Core");
            }
        }

        // EF Core fallback
        var monthCases = await _db.AuditCases.AsNoTracking()
            .Where(c => c.CreatedAt >= startOfMonth)
            .ToListAsync();

        return new AuditDashboardDto
        {
            TotalThisMonth = monthCases.Count,
            ApprovedCount = monthCases.Count(c => c.Status == "Approved"),
            RejectedCount = monthCases.Count(c => c.Status == "Rejected"),
            PendingCount = monthCases.Count(c => c.Status == "Pending"),
            TotalVariance = Math.Round(
                monthCases.Sum(c => Math.Abs(c.Variance)),
                2,
                MidpointRounding.AwayFromZero)
        };
    }

    public async Task<(List<AuditBatchDto> Data, int TotalCount)> GetBatches(string? auditType, int page, int pageSize)
    {
        var conn = TryGetDbConnection();
        if (conn != null)
        {
            try
            {
                return await GetBatchesDapper(conn, auditType, page, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Dapper audit batches query failed, falling back to EF Core");
            }
        }

        return await GetBatchesFallback(auditType, page, pageSize);
    }

    private static async Task<(List<AuditBatchDto> Data, int TotalCount)> GetBatchesDapper(IDbConnection conn, string? auditType, int page, int pageSize)
    {
        var countSql = new StringBuilder("SELECT COUNT(*) FROM AuditBatches WHERE 1=1");
        var sql = new StringBuilder("SELECT * FROM AuditBatches WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(auditType))
        {
            sql.Append(" AND AuditType = @AuditType");
            countSql.Append(" AND AuditType = @AuditType");
            parameters.Add("AuditType", auditType);
        }

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql.ToString(), parameters);

        sql.Append(" ORDER BY UploadDate DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");
        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var batches = await conn.QueryAsync<AuditBatch>(sql.ToString(), parameters);
        return (batches.Select(b => new AuditBatchDto
        {
            Id = b.Id,
            UploadDate = b.UploadDate,
            FileName = b.FileName,
            AuditType = b.AuditType,
            TotalCount = b.TotalCount,
            ProcessedCount = b.ProcessedCount,
            PendingCount = b.TotalCount - b.ProcessedCount,
            Status = b.Status,
            UploadedBy = b.UploadedBy
        }).ToList(), totalCount);
    }

    private async Task<(List<AuditBatchDto> Data, int TotalCount)> GetBatchesFallback(string? auditType, int page, int pageSize)
    {
        var query = _db.AuditBatches.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(auditType))
            query = query.Where(b => b.AuditType == auditType);

        var totalCount = await query.CountAsync();
        var batches = await query
            .OrderByDescending(b => b.UploadDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(b => b.Cases)
            .ToListAsync();

        return (batches.Select(b => new AuditBatchDto
        {
            Id = b.Id,
            UploadDate = b.UploadDate,
            FileName = b.FileName,
            AuditType = b.AuditType,
            TotalCount = b.TotalCount,
            ProcessedCount = b.ProcessedCount,
            PendingCount = b.TotalCount - b.ProcessedCount,
            Status = b.Status,
            UploadedBy = b.UploadedBy
        }).ToList(), totalCount);
    }

    public async Task<List<AuditCaseResultDto>> GetBatchCases(int batchId)
    {
        var conn = TryGetDbConnection();
        if (conn != null)
        {
            try
            {
                const string sql = "SELECT * FROM AuditCases WHERE BatchId = @BatchId ORDER BY CreatedAt DESC";
                var cases = await conn.QueryAsync<AuditCase>(sql, new { BatchId = batchId });
                return cases.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Dapper audit batch-cases query failed, falling back to EF Core");
            }
        }

        var efCases = await _db.AuditCases.AsNoTracking()
            .Where(c => c.BatchId == batchId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return efCases.Select(MapToDto).ToList();
    }

    private static AuditCaseResultDto MapToDto(AuditCase c) => new()
    {
        Id = c.Id,
        PolicyNumber = c.PolicyNumber,
        ProductName = c.ProductName,
        Uin = c.Uin,
        PolicyAnniversary = c.PolicyAnniversary,
        AuditType = c.AuditType,
        InputMode = c.InputMode,
        CoreSystemAmount = c.CoreSystemAmount,
        PrecisionProAmount = c.PrecisionProAmount,
        Variance = c.Variance,
        Status = c.Status,
        Remarks = c.Remarks,
        ProductVersion = c.ProductVersion,
        FactorVersion = c.FactorVersion,
        FormulaVersion = c.FormulaVersion,
        CalculationSource = c.CalculationSource,
        CalculatedAt = c.CalculatedAt,
        CreatedBy = c.CreatedBy,
        CreatedAt = c.CreatedAt
    };

    private static string MaskPolicy(string policyNumber)
    {
        if (string.IsNullOrWhiteSpace(policyNumber) || policyNumber.Length <= 4) return "****";
        var last4 = policyNumber[^4..];
        return $"****{last4}";
    }
}
