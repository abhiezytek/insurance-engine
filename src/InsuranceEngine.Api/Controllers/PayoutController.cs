using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Services;
using InsuranceEngine.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceEngine.Api.Controllers;

/// <summary>Payout verification module — 2-level approval workflow with CSV/JSON export.</summary>
[ApiController]
[Route("api/payout")]
[Produces("application/json")]
[Authorize(Policy = "CanViewAudit")]
[RequireRoleHeader("Admin", "SuperAdmin", "Auditor", "AuditUser", "Operations", "Checker", "Authorizer")]
public class PayoutController : ControllerBase
{
    private readonly IPayoutService _payoutService;

    public PayoutController(IPayoutService payoutService)
    {
        _payoutService = payoutService;
    }

    // ─── Single policy search & verify ───────────────────────────────────────

    /// <summary>Search a single policy and create a payout verification case.</summary>
    [HttpPost("search")]
    [Authorize(Roles = "Operations,Checker,Authorizer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PayoutCaseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchPolicy([FromBody] PayoutSearchRequest req)
    {
        var userId = User.Identity?.Name ?? "Anonymous";
        var result = await _payoutService.SearchAndVerify(req.PolicyNumber, req.PayoutType, userId);
        return Ok(result);
    }

    // ─── 2-Level Approval: Checker ───────────────────────────────────────────

    /// <summary>Checker approves a payout case (level 1).</summary>
    [HttpPost("checker/approve")]
    [Authorize(Roles = "Checker,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PayoutCaseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckerApprove([FromBody] PayoutDecisionRequest req)
    {
        var userId = User.Identity?.Name ?? "Anonymous";
        var result = await _payoutService.CheckerApprove(req.CaseId, req.Remarks, userId);
        return Ok(result);
    }

    /// <summary>Checker rejects a payout case (level 1).</summary>
    [HttpPost("checker/reject")]
    [Authorize(Roles = "Checker,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PayoutCaseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckerReject([FromBody] PayoutDecisionRequest req)
    {
        var userId = User.Identity?.Name ?? "Anonymous";
        var result = await _payoutService.CheckerReject(req.CaseId, req.Remarks, userId);
        return Ok(result);
    }

    // ─── 2-Level Approval: Authorizer ────────────────────────────────────────

    /// <summary>Authorizer approves a payout case (level 2) — pushes to core system.</summary>
    [HttpPost("authorizer/approve")]
    [Authorize(Roles = "Authorizer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PayoutCaseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AuthorizerApprove([FromBody] PayoutDecisionRequest req)
    {
        var userId = User.Identity?.Name ?? "Anonymous";
        var result = await _payoutService.AuthorizerApprove(req.CaseId, req.Remarks, userId);
        return Ok(result);
    }

    /// <summary>Authorizer rejects a payout case (level 2).</summary>
    [HttpPost("authorizer/reject")]
    [Authorize(Roles = "Authorizer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PayoutCaseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AuthorizerReject([FromBody] PayoutDecisionRequest req)
    {
        var userId = User.Identity?.Name ?? "Anonymous";
        var result = await _payoutService.AuthorizerReject(req.CaseId, req.Remarks, userId);
        return Ok(result);
    }

    // ─── Bulk decisions ──────────────────────────────────────────────────────

    /// <summary>Bulk checker approve for multiple payout cases.</summary>
    [HttpPost("checker/bulk-approve")]
    [Authorize(Roles = "Checker,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(List<PayoutCaseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkCheckerApprove([FromBody] PayoutBulkDecisionRequest req)
    {
        if (req.CaseIds.Count > 100)
            return BadRequest(new { error = "Maximum 100 cases per bulk request." });

        var userId = User.Identity?.Name ?? "Anonymous";
        var results = new List<PayoutCaseDto>();
        foreach (var caseId in req.CaseIds)
        {
            try { results.Add(await _payoutService.CheckerApprove(caseId, req.Remarks, userId)); }
            catch { /* skip failed cases */ }
        }
        return Ok(results);
    }

    /// <summary>Bulk authorizer approve for multiple payout cases.</summary>
    [HttpPost("authorizer/bulk-approve")]
    [Authorize(Roles = "Authorizer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(List<PayoutCaseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkAuthorizerApprove([FromBody] PayoutBulkDecisionRequest req)
    {
        if (req.CaseIds.Count > 100)
            return BadRequest(new { error = "Maximum 100 cases per bulk request." });

        var userId = User.Identity?.Name ?? "Anonymous";
        var results = new List<PayoutCaseDto>();
        foreach (var caseId in req.CaseIds)
        {
            try { results.Add(await _payoutService.AuthorizerApprove(caseId, req.Remarks, userId)); }
            catch { /* skip failed cases */ }
        }
        return Ok(results);
    }

    // ─── Queries ─────────────────────────────────────────────────────────────

    /// <summary>Get payout cases with optional filters.</summary>
    [HttpGet("cases")]
    [ProducesResponseType(typeof(List<PayoutCaseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCases(
        [FromQuery] string? payoutType,
        [FromQuery] string? status,
        [FromQuery] string? inputMode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        pageSize = Math.Min(Math.Max(pageSize, 1), 100);
        page = Math.Max(page, 1);
        var result = await _payoutService.GetCases(payoutType, status, inputMode, page, pageSize);
        return Ok(result);
    }

    /// <summary>Payout dashboard summary statistics.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(PayoutDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _payoutService.GetDashboard();
        return Ok(result);
    }

    /// <summary>Get payout batches.</summary>
    [HttpGet("batches")]
    [ProducesResponseType(typeof(List<PayoutBatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBatches(
        [FromQuery] string? payoutType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _payoutService.GetBatches(payoutType, page, pageSize);
        return Ok(result);
    }

    /// <summary>Get cases for a specific batch.</summary>
    [HttpGet("batches/{batchId}/cases")]
    [ProducesResponseType(typeof(List<PayoutCaseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBatchCases(int batchId)
    {
        var result = await _payoutService.GetBatchCases(batchId);
        return Ok(result);
    }

    // ─── Batch: System Generate ──────────────────────────────────────────────

    /// <summary>Generate a payout batch from system-detected due policies.</summary>
    [HttpPost("batch/generate")]
    [Authorize(Roles = "Operations,Checker,Authorizer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PayoutBatchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateBatch([FromBody] PayoutBatchGenerateRequest req)
    {
        var userId = User.Identity?.Name ?? "Anonymous";
        var result = await _payoutService.GenerateBatch(
            req.PayoutType, req.MaturityDateFrom, req.MaturityDateTo, req.MaxRecords, userId);
        return Ok(result);
    }

    // ─── Batch: File Upload (CSV/Excel) ──────────────────────────────────────

    /// <summary>Upload a CSV/Excel file for batch payout processing.</summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Operations,Checker,Authorizer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PayoutBatchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string payoutType = "Maturity")
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        var allowedExtensions = new[] { ".csv", ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { error = "Only .csv and .xlsx files are supported." });

        const long maxFileSize = 10 * 1024 * 1024; // 10 MB
        if (file.Length > maxFileSize)
            return BadRequest(new { error = "File exceeds 10 MB limit." });

        var userId = User.Identity?.Name ?? "Anonymous";
        var result = await _payoutService.ProcessUploadedFile(
            file.OpenReadStream(), file.FileName, payoutType, userId);
        return Ok(result);
    }

    /// <summary>Download a CSV template for payout batch upload.</summary>
    [HttpGet("template")]
    public IActionResult DownloadTemplate()
    {
        var csv = "PolicyNumber\nSAMPLE001\nSAMPLE002\n";
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "payout_upload_template.csv");
    }

    // ─── File Export (CSV / JSON) ────────────────────────────────────────────

    /// <summary>Export payout cases as CSV or JSON file.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportCases(
        [FromQuery] int? batchId,
        [FromQuery] string format = "CSV")
    {
        if (!format.Equals("CSV", StringComparison.OrdinalIgnoreCase) &&
            !format.Equals("JSON", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Invalid format. Use CSV or JSON." });

        var userId = User.Identity?.Name ?? "Anonymous";
        var (content, fileName, contentType) = await _payoutService.ExportCases(batchId, format, userId);
        return File(content, contentType, fileName);
    }
}
