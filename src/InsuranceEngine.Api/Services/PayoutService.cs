using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Services;

public class PayoutService : IPayoutService
{
    private readonly InsuranceDbContext _db;
    private readonly ICoreSystemGateway _gateway;
    private readonly IBenefitCalculationService _calcService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PayoutService> _logger;

    public PayoutService(
        InsuranceDbContext db,
        ICoreSystemGateway gateway,
        IBenefitCalculationService calcService,
        INotificationService notificationService,
        ILogger<PayoutService> logger)
    {
        _db = db;
        _gateway = gateway;
        _calcService = calcService;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ─── Search & Verify ─────────────────────────────────────────────────────

    public async Task<PayoutCaseDto> SearchAndVerify(string policyNumber, string payoutType, string userId)
    {
        _logger.LogInformation("Payout search for {PolicyNumber}, type {PayoutType}", policyNumber, payoutType);

        var policy = await _gateway.FetchPolicyByPolicyNumber(policyNumber)
            ?? throw new InvalidOperationException($"Policy {policyNumber} not found in core system.");

        var coreResult = await _gateway.FetchCoreSystemAmount(policyNumber, payoutType);

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

        decimal precisionProAmount = payoutType switch
        {
            "Surrender" => biResponse.YearlyTable.LastOrDefault()?.SurrenderValue ?? 0m,
            _ => biResponse.GuaranteedMaturityBenefit
        };

        decimal variance = Round(coreResult.Amount - precisionProAmount);
        decimal variancePct = precisionProAmount != 0
            ? Math.Round((variance / precisionProAmount) * 100m, 4, MidpointRounding.AwayFromZero)
            : 0m;

        var payoutCase = new PayoutCase
        {
            PolicyNumber = policyNumber,
            ProductName = policy.ProductName,
            Uin = policy.Uin,
            PayoutType = payoutType,
            InputMode = "Single",
            CoreSystemAmount = coreResult.Amount,
            PrecisionProAmount = precisionProAmount,
            Variance = variance,
            VariancePct = variancePct,
            Status = "Pending",
            PolicyStartDate = policy.PolicyAnniversary,
            AnnualPremium = policy.AnnualPremium,
            PolicyTerm = policy.PolicyTerm,
            PremiumPayingTerm = policy.PremiumPayingTerm,
            ProductVersion = biResponse.ProductVersion ?? "v-default",
            FactorVersion = biResponse.FactorVersion ?? "table-default",
            FormulaVersion = biResponse.FormulaVersion ?? "v-default",
            CalculationSource = "PrecisionPro",
            CalculatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.PayoutCases.Add(payoutCase);
        await _db.SaveChangesAsync();

        _db.PayoutWorkflowHistories.Add(new PayoutWorkflowHistory
        {
            PayoutCaseId = payoutCase.Id,
            Action = "Created",
            FromStatus = null,
            ToStatus = "Pending",
            PerformedBy = userId,
            PerformedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("PayoutCase {CaseId} created for {PolicyNumber}, variance {Variance}",
            payoutCase.Id, MaskPolicy(policyNumber), variance);

        // Notify Checkers of new pending case
        _ = _notificationService.NotifyRoleAsync("Checker",
            $"New payout case {policyNumber} ({payoutType}) pending your approval.",
            "PayoutVerification", payoutCase.Id.ToString());

        return MapToDto(payoutCase);
    }

    // ─── 2-Level Approval: Checker ───────────────────────────────────────────

    public async Task<PayoutCaseDto> CheckerApprove(int caseId, string? remarks, string userId)
    {
        var payoutCase = await _db.PayoutCases.FindAsync(caseId)
            ?? throw new InvalidOperationException($"Payout case {caseId} not found.");

        if (payoutCase.Status != "Pending")
            throw new InvalidOperationException($"Case {caseId} is not in Pending status (current: {payoutCase.Status}).");

        // GAP 1.3: Maker cannot be checker
        if (payoutCase.CreatedBy == userId)
            throw new InvalidOperationException("Maker cannot approve their own submission.");

        var fromStatus = payoutCase.Status;
        payoutCase.Status = "CheckerApproved";
        payoutCase.Remarks = remarks;

        _db.PayoutWorkflowHistories.Add(new PayoutWorkflowHistory
        {
            PayoutCaseId = caseId,
            Action = "CheckerApproved",
            FromStatus = fromStatus,
            ToStatus = "CheckerApproved",
            Remarks = remarks,
            PerformedBy = userId,
            PerformedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("PayoutCase {CaseId} checker-approved by {UserId}", caseId, userId);

        // Notify Authorizers that case is pending L2
        _ = _notificationService.NotifyRoleAsync("Authorizer",
            $"Case {payoutCase.PolicyNumber} approved by Checker, pending your authorization.",
            "PayoutVerification", caseId.ToString());

        return MapToDto(payoutCase);
    }

    public async Task<PayoutCaseDto> CheckerReject(int caseId, string? remarks, string userId)
    {
        var payoutCase = await _db.PayoutCases.FindAsync(caseId)
            ?? throw new InvalidOperationException($"Payout case {caseId} not found.");

        if (payoutCase.Status != "Pending")
            throw new InvalidOperationException($"Case {caseId} is not in Pending status (current: {payoutCase.Status}).");

        // GAP 1.3: Maker cannot be checker
        if (payoutCase.CreatedBy == userId)
            throw new InvalidOperationException("Maker cannot reject their own submission.");

        var fromStatus = payoutCase.Status;
        payoutCase.Status = "CheckerRejected";
        payoutCase.Remarks = remarks;

        _db.PayoutWorkflowHistories.Add(new PayoutWorkflowHistory
        {
            PayoutCaseId = caseId,
            Action = "CheckerRejected",
            FromStatus = fromStatus,
            ToStatus = "CheckerRejected",
            Remarks = remarks,
            PerformedBy = userId,
            PerformedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("PayoutCase {CaseId} checker-rejected by {UserId}", caseId, userId);

        // Notify the submitter of rejection
        _ = _notificationService.CreateAsync(payoutCase.CreatedBy,
            $"Your case {payoutCase.PolicyNumber} was rejected by Checker. Reason: {remarks ?? "No remarks"}",
            "PayoutVerification", caseId.ToString());

        return MapToDto(payoutCase);
    }

    // ─── 2-Level Approval: Authorizer ────────────────────────────────────────

    public async Task<PayoutCaseDto> AuthorizerApprove(int caseId, string? remarks, string userId)
    {
        var payoutCase = await _db.PayoutCases.FindAsync(caseId)
            ?? throw new InvalidOperationException($"Payout case {caseId} not found.");

        if (payoutCase.Status != "CheckerApproved")
            throw new InvalidOperationException($"Case {caseId} is not in CheckerApproved status (current: {payoutCase.Status}).");

        // GAP 1.3: Checker cannot be authorizer
        var checkerStep = await _db.PayoutWorkflowHistories
            .Where(h => h.PayoutCaseId == caseId && h.Action == "CheckerApproved")
            .OrderByDescending(h => h.PerformedAt)
            .FirstOrDefaultAsync();
        if (checkerStep != null && checkerStep.PerformedBy == userId)
            throw new InvalidOperationException("L1 Checker cannot be L2 Authorizer for the same case.");

        var fromStatus = payoutCase.Status;
        payoutCase.Status = "Authorized";
        payoutCase.Remarks = remarks;

        var pushResult = await _gateway.PushApprovalToCore(
            payoutCase.PolicyNumber,
            payoutCase.PayoutType,
            payoutCase.PrecisionProAmount,
            remarks,
            userId);

        _db.PayoutWorkflowHistories.Add(new PayoutWorkflowHistory
        {
            PayoutCaseId = caseId,
            Action = "Authorized",
            FromStatus = fromStatus,
            ToStatus = "Authorized",
            Remarks = remarks,
            PerformedBy = userId,
            PerformedAt = DateTime.UtcNow,
            PushStatus = pushResult.Success ? "Success" : "Failed",
            PushReferenceNumber = pushResult.ReferenceNumber,
            PushErrorMessage = pushResult.ErrorMessage
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("PayoutCase {CaseId} authorized by {UserId}, push ref={Ref}",
            caseId, userId, pushResult.ReferenceNumber);
        return MapToDto(payoutCase);
    }

    public async Task<PayoutCaseDto> AuthorizerReject(int caseId, string? remarks, string userId)
    {
        var payoutCase = await _db.PayoutCases.FindAsync(caseId)
            ?? throw new InvalidOperationException($"Payout case {caseId} not found.");

        if (payoutCase.Status != "CheckerApproved")
            throw new InvalidOperationException($"Case {caseId} is not in CheckerApproved status (current: {payoutCase.Status}).");

        // GAP 1.3: Checker cannot be authorizer
        var checkerStep = await _db.PayoutWorkflowHistories
            .Where(h => h.PayoutCaseId == caseId && h.Action == "CheckerApproved")
            .OrderByDescending(h => h.PerformedAt)
            .FirstOrDefaultAsync();
        if (checkerStep != null && checkerStep.PerformedBy == userId)
            throw new InvalidOperationException("L1 Checker cannot be L2 Authorizer for the same case.");

        var fromStatus = payoutCase.Status;
        payoutCase.Status = "Rejected";
        payoutCase.Remarks = remarks;

        _db.PayoutWorkflowHistories.Add(new PayoutWorkflowHistory
        {
            PayoutCaseId = caseId,
            Action = "Rejected",
            FromStatus = fromStatus,
            ToStatus = "Rejected",
            Remarks = remarks,
            PerformedBy = userId,
            PerformedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("PayoutCase {CaseId} authorizer-rejected by {UserId}", caseId, userId);

        // Notify the submitter of rejection
        _ = _notificationService.CreateAsync(payoutCase.CreatedBy,
            $"Your case {payoutCase.PolicyNumber} was rejected by Authorizer. Reason: {remarks ?? "No remarks"}",
            "PayoutVerification", caseId.ToString());

        return MapToDto(payoutCase);
    }

    // ─── Queries (using Dapper for read-heavy paths — GAP 1.1) ────────────────

    private IDbConnection? TryGetDbConnection()
    {
        try { return _db.Database.GetDbConnection(); }
        catch { return null; }
    }

    public async Task<List<PayoutCaseDto>> GetCases(string? payoutType, string? status, string? inputMode, int page, int pageSize)
    {
        try
        {
            return await GetCasesDapper(payoutType, status, inputMode, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Dapper cases query failed, falling back to EF Core");
            return await GetCasesFallback(payoutType, status, inputMode, page, pageSize);
        }
    }

    private async Task<List<PayoutCaseDto>> GetCasesDapper(string? payoutType, string? status, string? inputMode, int page, int pageSize)
    {
        var conn = TryGetDbConnection();
        if (conn == null) return await GetCasesFallback(payoutType, status, inputMode, page, pageSize);
        var sql = new StringBuilder(@"
            SELECT c.*, w.Id AS WId, w.PayoutCaseId, w.Action, w.FromStatus, w.ToStatus,
                   w.Remarks AS WRemarks, w.PerformedBy, w.PerformedAt
            FROM PayoutCases c
            LEFT JOIN PayoutWorkflowHistories w ON w.PayoutCaseId = c.Id
            WHERE 1=1");

        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(payoutType))
        {
            sql.Append(" AND c.PayoutType = @PayoutType");
            parameters.Add("PayoutType", payoutType);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            sql.Append(" AND c.Status = @Status");
            parameters.Add("Status", status);
        }
        if (!string.IsNullOrWhiteSpace(inputMode))
        {
            sql.Append(" AND c.InputMode = @InputMode");
            parameters.Add("InputMode", inputMode);
        }

        sql.Append(" ORDER BY c.CreatedAt DESC");

        var caseDict = new Dictionary<int, PayoutCaseDto>();

        await conn.QueryAsync<PayoutCase, PayoutWorkflowHistory?, PayoutCaseDto>(
            sql.ToString(),
            (c, w) =>
            {
                if (!caseDict.TryGetValue(c.Id, out var dto))
                {
                    dto = MapToDto(c);
                    dto.WorkflowHistory = new List<PayoutWorkflowStepDto>();
                    caseDict[c.Id] = dto;
                }
                if (w != null)
                {
                    dto.WorkflowHistory.Add(new PayoutWorkflowStepDto
                    {
                        Id = w.Id,
                        Action = w.Action,
                        FromStatus = w.FromStatus,
                        ToStatus = w.ToStatus,
                        Remarks = w.Remarks,
                        PerformedBy = w.PerformedBy,
                        PerformedAt = w.PerformedAt
                    });
                }
                return dto;
            },
            parameters,
            splitOn: "WId");

        return caseDict.Values
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    private async Task<List<PayoutCaseDto>> GetCasesFallback(string? payoutType, string? status, string? inputMode, int page, int pageSize)
    {
        var query = _db.PayoutCases.Include(c => c.WorkflowHistory).AsQueryable();

        if (!string.IsNullOrWhiteSpace(payoutType))
            query = query.Where(c => c.PayoutType == payoutType);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(inputMode))
            query = query.Where(c => c.InputMode == inputMode);

        var cases = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return cases.Select(MapToDto).ToList();
    }

    public async Task<PayoutDashboardDto> GetDashboard()
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var conn = TryGetDbConnection();
        if (conn == null) return await GetDashboardFallback(startOfMonth);

        const string sql = @"
            SELECT
                COUNT(*)                                                           AS TotalThisMonth,
                SUM(CASE WHEN Status = 'Pending'           THEN 1 ELSE 0 END)     AS PendingCount,
                SUM(CASE WHEN Status = 'CheckerApproved'   THEN 1 ELSE 0 END)     AS CheckerApprovedCount,
                SUM(CASE WHEN Status = 'Authorized'        THEN 1 ELSE 0 END)     AS AuthorizedCount,
                SUM(CASE WHEN Status IN ('CheckerRejected','Rejected') THEN 1 ELSE 0 END) AS RejectedCount,
                SUM(CASE WHEN ABS(VariancePct) <= 1        THEN 1 ELSE 0 END)     AS MatchCount,
                SUM(CASE WHEN ABS(VariancePct) > 1         THEN 1 ELSE 0 END)     AS MismatchCount,
                ISNULL(SUM(ABS(Variance)), 0)                                      AS TotalVariance
            FROM PayoutCases
            WHERE CreatedAt >= @StartOfMonth";

        try
        {
            var result = await conn.QueryFirstOrDefaultAsync<PayoutDashboardDto>(sql, new { StartOfMonth = startOfMonth });
            return result ?? new PayoutDashboardDto();
        }
        catch (Exception ex)
        {
            // Fallback for InMemory DB in tests (no SQL Server available)
            _logger.LogDebug(ex, "Dapper dashboard query failed, falling back to EF Core");
            return await GetDashboardFallback(startOfMonth);
        }
    }

    private async Task<PayoutDashboardDto> GetDashboardFallback(DateTime startOfMonth)
    {
        var monthCases = await _db.PayoutCases
            .Where(c => c.CreatedAt >= startOfMonth)
            .ToListAsync();

        return new PayoutDashboardDto
        {
            TotalThisMonth = monthCases.Count,
            PendingCount = monthCases.Count(c => c.Status == "Pending"),
            CheckerApprovedCount = monthCases.Count(c => c.Status == "CheckerApproved"),
            AuthorizedCount = monthCases.Count(c => c.Status == "Authorized"),
            RejectedCount = monthCases.Count(c => c.Status is "CheckerRejected" or "Rejected"),
            MatchCount = monthCases.Count(c => Math.Abs(c.VariancePct) <= 1m),
            MismatchCount = monthCases.Count(c => Math.Abs(c.VariancePct) > 1m),
            TotalVariance = Round(monthCases.Sum(c => Math.Abs(c.Variance)))
        };
    }

    public async Task<List<PayoutBatchDto>> GetBatches(string? payoutType, int page, int pageSize)
    {
        var conn = TryGetDbConnection();
        if (conn == null) return await GetBatchesFallback(payoutType, page, pageSize);
        var sql = new StringBuilder("SELECT * FROM PayoutBatches WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(payoutType))
        {
            sql.Append(" AND PayoutType = @PayoutType");
            parameters.Add("PayoutType", payoutType);
        }

        sql.Append(" ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");
        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        try
        {
            var batches = await conn.QueryAsync<PayoutBatch>(sql.ToString(), parameters);
            return batches.Select(MapBatchToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Dapper batches query failed, falling back to EF Core");
            return await GetBatchesFallback(payoutType, page, pageSize);
        }
    }

    private async Task<List<PayoutBatchDto>> GetBatchesFallback(string? payoutType, int page, int pageSize)
    {
        var query = _db.PayoutBatches.AsQueryable();

        if (!string.IsNullOrWhiteSpace(payoutType))
            query = query.Where(b => b.PayoutType == payoutType);

        var batches = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return batches.Select(MapBatchToDto).ToList();
    }

    public async Task<List<PayoutCaseDto>> GetBatchCases(int batchId)
    {
        var cases = await _db.PayoutCases
            .Include(c => c.WorkflowHistory)
            .Where(c => c.BatchId == batchId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return cases.Select(MapToDto).ToList();
    }

    // ─── Batch Generation ────────────────────────────────────────────────────

    public async Task<PayoutBatchDto> GenerateBatch(string payoutType, DateTime? fromDate, DateTime? toDate, int maxRecords, string userId)
    {
        _logger.LogInformation("Generating payout batch: type={PayoutType}, max={Max}", payoutType, maxRecords);

        var batch = new PayoutBatch
        {
            BatchType = "SystemGenerated",
            PayoutType = payoutType,
            Status = "Processing",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.PayoutBatches.Add(batch);
        await _db.SaveChangesAsync();

        // Stub: generate sample policies from mock gateway
        var policyNumbers = Enumerable.Range(1, Math.Min(maxRecords, 100))
            .Select(i => $"SYS{i:D6}")
            .ToList();

        batch.TotalCount = policyNumbers.Count;
        int matchCount = 0, mismatchCount = 0;

        foreach (var policyNumber in policyNumbers)
        {
            try
            {
                var caseDto = await SearchAndVerify(policyNumber, payoutType, userId);
                var payoutCase = await _db.PayoutCases.FindAsync(caseDto.Id);
                if (payoutCase != null)
                {
                    payoutCase.BatchId = batch.Id;
                    payoutCase.InputMode = "SystemGenerated";
                }

                if (Math.Abs(caseDto.VariancePct) <= 1m) matchCount++;
                else mismatchCount++;

                batch.ProcessedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process policy {PolicyNumber} in batch", policyNumber);
            }
        }

        batch.MatchCount = matchCount;
        batch.MismatchCount = mismatchCount;
        batch.Status = batch.ProcessedCount == batch.TotalCount ? "Processed" : "PartiallyProcessed";
        await _db.SaveChangesAsync();

        return MapBatchToDto(batch);
    }

    // ─── File Upload (CSV/Excel) ─────────────────────────────────────────────

    public async Task<PayoutBatchDto> ProcessUploadedFile(Stream fileStream, string fileName, string payoutType, string userId)
    {
        // Sanitize filename to prevent path traversal
        var safeFileName = Path.GetFileName(fileName);
        _logger.LogInformation("Processing uploaded file {FileName}", safeFileName);

        var batch = new PayoutBatch
        {
            BatchType = "FileUpload",
            FileName = safeFileName,
            PayoutType = payoutType,
            Status = "Processing",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.PayoutBatches.Add(batch);
        await _db.SaveChangesAsync();

        // Compute SHA256 hash of uploaded file for integrity (GAP 1.5)
        var uploadBytes = new byte[fileStream.Length];
        fileStream.Position = 0;
        await fileStream.ReadExactlyAsync(uploadBytes.AsMemory(0, (int)fileStream.Length));
        fileStream.Position = 0;
        var uploadHash = ComputeSha256Hash(uploadBytes);

        // Record the uploaded file
        _db.PayoutFiles.Add(new PayoutFile
        {
            BatchId = batch.Id,
            FileName = safeFileName,
            FileFormat = Path.GetExtension(safeFileName).TrimStart('.').ToUpperInvariant(),
            FileType = "Upload",
            FileSizeBytes = fileStream.Length,
            GeneratedBy = userId,
            GeneratedAt = DateTime.UtcNow,
            FileHash = uploadHash
        });
        await _db.SaveChangesAsync();

        // Parse policy numbers from CSV
        var policyNumbers = new List<string>();
        using (var reader = new StreamReader(fileStream))
        {
            var header = await reader.ReadLineAsync(); // skip header
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(',', '\t');
                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                    policyNumbers.Add(parts[0].Trim().Trim('"'));
            }
        }

        batch.TotalCount = policyNumbers.Count;
        int matchCount = 0, mismatchCount = 0;

        foreach (var policyNumber in policyNumbers)
        {
            try
            {
                var caseDto = await SearchAndVerify(policyNumber, payoutType, userId);
                var payoutCase = await _db.PayoutCases.FindAsync(caseDto.Id);
                if (payoutCase != null)
                {
                    payoutCase.BatchId = batch.Id;
                    payoutCase.InputMode = "FileUpload";
                }

                if (Math.Abs(caseDto.VariancePct) <= 1m) matchCount++;
                else mismatchCount++;

                batch.ProcessedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process policy {PolicyNumber} from file", policyNumber);
            }
        }

        batch.MatchCount = matchCount;
        batch.MismatchCount = mismatchCount;
        batch.Status = batch.ProcessedCount == batch.TotalCount ? "Processed" : "PartiallyProcessed";
        await _db.SaveChangesAsync();

        return MapBatchToDto(batch);
    }

    // ─── Export (CSV / JSON) ─────────────────────────────────────────────────

    public async Task<(byte[] Content, string FileName, string ContentType)> ExportCases(int? batchId, string format, string userId)
    {
        var query = _db.PayoutCases.AsQueryable();
        if (batchId.HasValue)
            query = query.Where(c => c.BatchId == batchId.Value);

        var cases = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

        byte[] content;
        string fileName;
        string contentType;

        if (format.Equals("JSON", StringComparison.OrdinalIgnoreCase))
        {
            var exportData = cases.Select(c => new
            {
                c.Id,
                c.PolicyNumber,
                c.ProductName,
                c.Uin,
                c.PayoutType,
                c.CoreSystemAmount,
                c.PrecisionProAmount,
                c.Variance,
                c.VariancePct,
                c.Status,
                c.CreatedAt
            });
            content = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true }));
            fileName = $"payout_export_{timestamp}.json";
            contentType = "application/json";
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id,PolicyNumber,ProductName,UIN,PayoutType,CoreSystemAmount,PrecisionProAmount,Variance,VariancePct,Status,CreatedAt");
            foreach (var c in cases)
            {
                sb.AppendLine(string.Join(",",
                    c.Id,
                    CsvEscape(c.PolicyNumber),
                    CsvEscape(c.ProductName),
                    CsvEscape(c.Uin),
                    CsvEscape(c.PayoutType),
                    c.CoreSystemAmount.ToString(CultureInfo.InvariantCulture),
                    c.PrecisionProAmount.ToString(CultureInfo.InvariantCulture),
                    c.Variance.ToString(CultureInfo.InvariantCulture),
                    c.VariancePct.ToString(CultureInfo.InvariantCulture),
                    CsvEscape(c.Status),
                    c.CreatedAt.ToString("o", CultureInfo.InvariantCulture)));
            }
            content = Encoding.UTF8.GetBytes(sb.ToString());
            fileName = $"payout_export_{timestamp}.csv";
            contentType = "text/csv";
        }

        // Record the exported file with SHA256 hash for integrity (GAP 1.5)
        var fileHash = ComputeSha256Hash(content);

        _db.PayoutFiles.Add(new PayoutFile
        {
            BatchId = batchId,
            FileName = fileName,
            FileFormat = format.ToUpperInvariant(),
            FileType = "Export",
            FileSizeBytes = content.Length,
            RecordCount = cases.Count,
            GeneratedBy = userId,
            GeneratedAt = DateTime.UtcNow,
            FileHash = fileHash
        });
        await _db.SaveChangesAsync();

        return (content, fileName, contentType);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static PayoutCaseDto MapToDto(PayoutCase c) => new()
    {
        Id = c.Id,
        PolicyNumber = c.PolicyNumber,
        ProductName = c.ProductName,
        Uin = c.Uin,
        PayoutType = c.PayoutType,
        InputMode = c.InputMode,
        BatchId = c.BatchId,
        CoreSystemAmount = c.CoreSystemAmount,
        PrecisionProAmount = c.PrecisionProAmount,
        Variance = c.Variance,
        VariancePct = c.VariancePct,
        Status = c.Status,
        Remarks = c.Remarks,
        PolicyStartDate = c.PolicyStartDate,
        PolicyMaturityDate = c.PolicyMaturityDate,
        SumAssured = c.SumAssured,
        AnnualPremium = c.AnnualPremium,
        PolicyTerm = c.PolicyTerm,
        PremiumPayingTerm = c.PremiumPayingTerm,
        ProductVersion = c.ProductVersion,
        FactorVersion = c.FactorVersion,
        FormulaVersion = c.FormulaVersion,
        CalculationSource = c.CalculationSource,
        CalculatedAt = c.CalculatedAt,
        CreatedBy = c.CreatedBy,
        CreatedAt = c.CreatedAt,
        WorkflowHistory = (c.WorkflowHistory ?? new()).Select(w => new PayoutWorkflowStepDto
        {
            Id = w.Id,
            Action = w.Action,
            FromStatus = w.FromStatus,
            ToStatus = w.ToStatus,
            Remarks = w.Remarks,
            PerformedBy = w.PerformedBy,
            PerformedAt = w.PerformedAt
        }).ToList()
    };

    private static PayoutBatchDto MapBatchToDto(PayoutBatch b) => new()
    {
        Id = b.Id,
        BatchType = b.BatchType,
        FileName = b.FileName,
        PayoutType = b.PayoutType,
        TotalCount = b.TotalCount,
        ProcessedCount = b.ProcessedCount,
        MatchCount = b.MatchCount,
        MismatchCount = b.MismatchCount,
        Status = b.Status,
        CreatedBy = b.CreatedBy,
        CreatedAt = b.CreatedAt
    };

    private static decimal Round(decimal v) =>
        Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private static string MaskPolicy(string policyNumber)
    {
        if (string.IsNullOrWhiteSpace(policyNumber) || policyNumber.Length <= 4) return "****";
        return $"****{policyNumber[^4..]}";
    }

    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        // Prevent CSV formula injection
        if (value.Length > 0 && (value[0] == '=' || value[0] == '+' || value[0] == '-' || value[0] == '@'))
            value = "'" + value;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string ComputeSha256Hash(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
