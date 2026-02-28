using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceEngine.Api.Controllers;

/// <summary>SUD Life Century Income – Benefit Illustration calculation endpoints.</summary>
[ApiController]
[Route("api/benefit-illustration")]
[Produces("application/json")]
public class BenefitIllustrationController : ControllerBase
{
    private readonly IBenefitCalculationService _svc;

    public BenefitIllustrationController(IBenefitCalculationService svc) => _svc = svc;

    /// <summary>Generate a full yearly Benefit Illustration table for a Century Income policy.</summary>
    /// <remarks>
    /// Computes SAD, GMB (with High Premium + Channel benefits), yearly GI, LI, SV, GSV, SSV, Death Benefit, and Maturity Benefit.
    ///
    /// **Sample request (Immediate, 7PPT/15PT, age 35):**
    /// ```json
    /// { "annualPremium": 50000, "ppt": 7, "policyTerm": 15, "entryAge": 35, "option": "Immediate", "channel": "Online" }
    /// ```
    /// </remarks>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(BenefitIllustrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BenefitIllustrationResponse>> Calculate([FromBody] BenefitIllustrationRequest request)
    {
        if (request.AnnualPremium <= 0) return BadRequest("Annual premium must be positive.");
        if (request.Ppt < 1 || request.Ppt > request.PolicyTerm) return BadRequest("PPT must be between 1 and Policy Term.");
        if (request.PolicyTerm < 5 || request.PolicyTerm > 40) return BadRequest("Policy Term must be between 5 and 40 years.");
        if (request.EntryAge < 0 || request.EntryAge > 65) return BadRequest("Entry age must be between 0 and 65.");

        var result = await _svc.CalculateAsync(request);
        return Ok(result);
    }
}
