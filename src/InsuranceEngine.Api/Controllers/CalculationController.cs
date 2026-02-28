using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Controllers;

/// <summary>Insurance benefit calculation endpoints.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CalculationController : ControllerBase
{
    private readonly InsuranceDbContext _db;
    private readonly FormulaEngine _formulaEngine;

    public CalculationController(InsuranceDbContext db, FormulaEngine formulaEngine)
    {
        _db = db;
        _formulaEngine = formulaEngine;
    }

    /// <summary>Run a traditional product calculation.</summary>
    /// <remarks>
    /// Evaluates all formulas for the specified product version in execution order.
    /// Formulas can reference input parameters and results of earlier formulas.
    ///
    /// **Sample request:**
    /// ```json
    /// {
    ///   "productCode": "CENTURY_INCOME",
    ///   "parameters": {
    ///     "AP": 10000, "SA": 100000, "PPT": 10, "PT": 20,
    ///     "Age": 35, "TotalPremiumPaid": 50000, "SurrenderValue": 40000
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Product code, optional version, and input parameters.</param>
    /// <returns>Computed formula results keyed by formula name.</returns>
    [HttpPost("traditional")]
    [ProducesResponseType(typeof(TraditionalCalculationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TraditionalCalculationResponse>> Calculate([FromBody] TraditionalCalculationRequest request)
    {
        var product = await _db.Products
            .Include(p => p.Versions)
                .ThenInclude(v => v.Formulas)
            .FirstOrDefaultAsync(p => p.Code == request.ProductCode);

        if (product == null)
            return NotFound($"Product '{request.ProductCode}' not found.");

        var version = request.Version != null
            ? product.Versions.FirstOrDefault(v => v.Version == request.Version)
            : product.Versions.Where(v => v.IsActive).OrderByDescending(v => v.EffectiveDate).FirstOrDefault();

        if (version == null)
            return NotFound($"No active version found for product '{request.ProductCode}'.");

        try
        {
            var results = _formulaEngine.Evaluate(version.Formulas, request.Parameters);
            return Ok(new TraditionalCalculationResponse
            {
                ProductCode = product.Code,
                Version = version.Version,
                Results = results
            });
        }
        catch (FormulaEngineException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
