using InsuranceEngine.Api.DTOs;

namespace InsuranceEngine.Api.Services;

public interface IAuditService
{
    Task<AuditCaseResultDto> ProcessSinglePolicy(string policyNumber, string auditType, string userId);
    Task<AuditCaseResultDto> ApproveCase(int caseId, string? remarks, string userId);
    Task<AuditCaseResultDto> RejectCase(int caseId, string? remarks, string userId);
    Task<(List<AuditCaseResultDto> Data, int TotalCount)> GetCases(string? auditType, string? status, string? inputMode, int page, int pageSize);
    Task<AuditDashboardDto> GetDashboard();
    Task<(List<AuditBatchDto> Data, int TotalCount)> GetBatches(string? auditType, int page, int pageSize);
    Task<List<AuditCaseResultDto>> GetBatchCases(int batchId);
}
