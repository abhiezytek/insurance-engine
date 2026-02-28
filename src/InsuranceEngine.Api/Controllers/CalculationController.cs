using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalculationController : ControllerBase
{
    private readonly InsuranceDbContext _db;
    private readonly FormulaEngine _formulaEngine;

    public CalculationController(InsuranceDbContext db, FormulaEngine formulaEngine)
    {
        _db = db;
        _formulaEngine = formulaEngine;
    }

    [HttpPost("traditional")]
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
