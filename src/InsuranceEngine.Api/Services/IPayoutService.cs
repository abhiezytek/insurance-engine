using InsuranceEngine.Api.DTOs;

namespace InsuranceEngine.Api.Services;

public interface IPayoutService
{
    Task<PayoutCaseDto> SearchAndVerify(string policyNumber, string payoutType, string userId);
    Task<PayoutCaseDto> CheckerApprove(int caseId, string? remarks, string userId);
    Task<PayoutCaseDto> CheckerReject(int caseId, string? remarks, string userId);
    Task<PayoutCaseDto> AuthorizerApprove(int caseId, string? remarks, string userId);
    Task<PayoutCaseDto> AuthorizerReject(int caseId, string? remarks, string userId);
    Task<(List<PayoutCaseDto> Data, int TotalCount)> GetCases(string? payoutType, string? status, string? inputMode, int page, int pageSize);
    Task<PayoutDashboardDto> GetDashboard();
    Task<(List<PayoutBatchDto> Data, int TotalCount)> GetBatches(string? payoutType, int page, int pageSize);
    Task<List<PayoutCaseDto>> GetBatchCases(int batchId);
    Task<PayoutBatchDto> GenerateBatch(string payoutType, DateTime? fromDate, DateTime? toDate, int maxRecords, string userId);
    Task<PayoutBatchDto> ProcessUploadedFile(Stream fileStream, string fileName, string payoutType, string userId);
    Task<(byte[] Content, string FileName, string ContentType)> ExportCases(int? batchId, string format, string userId);
}
