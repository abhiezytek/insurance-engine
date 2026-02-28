using InsuranceEngine.Api.DTOs;

namespace InsuranceEngine.Api.Services;

public interface IBenefitCalculationService
{
    Task<BenefitIllustrationResponse> CalculateAsync(BenefitIllustrationRequest request);
}
