using InsuranceEngine.Api.DTOs;

namespace InsuranceEngine.Api.Services;

public interface IUlipCalculationService
{
    /// <summary>
    /// Generate a full yearly ULIP Benefit Illustration for both 4% and 8% assumed return scenarios.
    /// The result is also persisted to the database using the policy number as the key.
    /// </summary>
    Task<UlipCalculationResponse> CalculateAsync(UlipCalculationRequest request);

    /// <summary>Retrieve a previously computed illustration by policy number.</summary>
    Task<UlipCalculationResponse?> GetByPolicyNumberAsync(string policyNumber);
}
