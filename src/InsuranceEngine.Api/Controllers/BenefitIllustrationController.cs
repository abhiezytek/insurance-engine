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
    private readonly IActivityAuditService _audit;

    public BenefitIllustrationController(IBenefitCalculationService svc, IActivityAuditService audit)
    {
        _svc = svc;
        _audit = audit;
    }

    /// <summary>
    /// Returns product configuration: allowed PPT values, PT options per PPT,
    /// sales channels, and payment modes. Table-driven and configurable.
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(EndowmentProductConfig), StatusCodes.Status200OK)]
    public IActionResult GetConfig()
    {
        return Ok(new EndowmentProductConfig());
    }

    /// <summary>Generate a full yearly Benefit Illustration table for a Century Income policy.</summary>
    /// <remarks>
    /// Computes SAD, GMB (with High Premium + Channel benefits), yearly GI, LI, SV, GSV, SSV, Death Benefit, and Maturity Benefit.
    ///
    /// **Sample request (Immediate, 7PPT/15PT, age 35):**
    /// ```json
    /// { "annualisedPremium": 50000, "ppt": 7, "policyTerm": 15, "entryAge": 35, "option": "Immediate", "channel": "Agency" }
    /// ```
    /// </remarks>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(BenefitIllustrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BenefitIllustrationResponse>> Calculate([FromBody] BenefitIllustrationRequest request)
    {
        var asOf = DateTime.UtcNow;

        if (request.EntryAge <= 0 && request.DateOfBirth != default)
            request.EntryAge = DeriveAge(request.DateOfBirth, asOf);

        if (request.AgeOfPolicyHolder.GetValueOrDefault() <= 0 && request.PolicyholderDateOfBirth.HasValue && request.PolicyholderDateOfBirth.Value != default)
            request.AgeOfPolicyHolder = DeriveAge(request.PolicyholderDateOfBirth.Value, asOf);

        if (request.LifeAssuredSameAsProposer)
        {
            request.PolicyholderDateOfBirth ??= request.DateOfBirth;
            request.AgeOfPolicyHolder ??= request.EntryAge;
            request.NameOfPolicyHolder = string.IsNullOrWhiteSpace(request.NameOfPolicyHolder)
                ? request.NameOfLifeAssured
                : request.NameOfPolicyHolder;
        }

        var premium = request.AnnualisedPremium ?? request.AnnualPremium;
        if (premium <= 0) return BadRequest("Premium must be positive.");
        if (request.Ppt < 1 || request.Ppt > request.PolicyTerm) return BadRequest("PPT must be between 1 and Policy Term.");
        if (request.PolicyTerm < 5 || request.PolicyTerm > 40) return BadRequest("Policy Term must be between 5 and 40 years.");
        if (request.EntryAge < 0 || request.EntryAge > 65) return BadRequest("Entry age must be between 0 and 65.");

        // Option validation
        var allowedOptions = new[] { "Immediate", "Deferred", "Twin", "Twin Income" };
        if (!string.IsNullOrWhiteSpace(request.Option) &&
            !allowedOptions.Contains(request.Option, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest($"Unsupported option '{request.Option}'. Allowed values: {string.Join(", ", allowedOptions)}.");
        }

        // Premium frequency validation
        var allowedFrequencies = new[] { "Yearly", "Half Yearly", "HalfYearly", "Quarterly", "Monthly" };
        if (!string.IsNullOrWhiteSpace(request.PremiumFrequency) &&
            !allowedFrequencies.Contains(request.PremiumFrequency, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest($"Unsupported premium frequency '{request.PremiumFrequency}'. Allowed values: Yearly, Half Yearly, Quarterly, Monthly.");
        }

        try
        {
            var result = await _svc.CalculateAsync(request);
            await _audit.LogAsync("BI", "GenerateIllustration",
                recordId: $"{request.Option ?? "Default"}/PPT{request.Ppt}/PT{request.PolicyTerm}");
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static int DeriveAge(DateTime dob, DateTime asOf)
    {
        var age = asOf.Year - dob.Year;
        if (dob.Date > asOf.AddYears(-age)) age--;
        return Math.Max(age, 0);
    }
}

/// <summary>Product configuration for the Endowment BI form (table-driven dropdowns).</summary>
public class EndowmentProductConfig
{
    /// <summary>Allowed Premium Paying Terms for this product.</summary>
    public int[] PptOptions { get; set; } = [7, 10, 12];

    /// <summary>Allowed Policy Term options per PPT.</summary>
    public Dictionary<string, int[]> PtOptionsByPpt { get; set; } = new()
    {
        ["7"] = [15, 20],
        ["10"] = [20, 25],
        ["12"] = [25]
    };

    /// <summary>Available sales channels.</summary>
    public string[] Channels { get; set; } =
    [
        "Corporate Agency",
        "Direct Marketing",
        "Online",
        "Broker",
        "Agency",
        "Web Aggregator",
        "Insurance Marketing Firm"
    ];

    /// <summary>Available premium payment modes.</summary>
    public string[] PaymentModes { get; set; } = ["Yearly", "Half Yearly", "Quarterly", "Monthly"];
}
