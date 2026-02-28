using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly InsuranceDbContext _db;
    private readonly FormulaEngine _formulaEngine;

    public AdminController(InsuranceDbContext db, FormulaEngine formulaEngine)
    {
        _db = db;
        _formulaEngine = formulaEngine;
    }

    // --- Products ---
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _db.Products.Include(p => p.Insurer).Include(p => p.Versions).ToListAsync();
        return Ok(products);
    }

    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _db.Products.Include(p => p.Insurer).Include(p => p.Versions).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest req)
    {
        var product = new Product { InsurerId = req.InsurerId, Name = req.Name, Code = req.Code, ProductType = req.ProductType };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    // --- Versions ---
    [HttpGet("versions")]
    public async Task<IActionResult> GetVersions([FromQuery] int? productId)
    {
        var q = _db.ProductVersions.AsQueryable();
        if (productId.HasValue) q = q.Where(v => v.ProductId == productId.Value);
        return Ok(await q.ToListAsync());
    }

    [HttpPost("versions")]
    public async Task<IActionResult> CreateVersion([FromBody] CreateProductVersionRequest req)
    {
        var version = new ProductVersion { ProductId = req.ProductId, Version = req.Version, IsActive = req.IsActive, EffectiveDate = req.EffectiveDate };
        _db.ProductVersions.Add(version);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetVersions), new { id = version.Id }, version);
    }

    // --- Parameters ---
    [HttpGet("parameters")]
    public async Task<IActionResult> GetParameters([FromQuery] int? productVersionId)
    {
        var q = _db.ProductParameters.AsQueryable();
        if (productVersionId.HasValue) q = q.Where(p => p.ProductVersionId == productVersionId.Value);
        return Ok(await q.ToListAsync());
    }

    [HttpPost("parameters")]
    public async Task<IActionResult> CreateParameter([FromBody] CreateParameterRequest req)
    {
        var param = new ProductParameter
        {
            ProductVersionId = req.ProductVersionId, Name = req.Name, DataType = req.DataType,
            IsRequired = req.IsRequired, DefaultValue = req.DefaultValue, Description = req.Description
        };
        _db.ProductParameters.Add(param);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetParameters), new { id = param.Id }, param);
    }

    [HttpDelete("parameters/{id}")]
    public async Task<IActionResult> DeleteParameter(int id)
    {
        var param = await _db.ProductParameters.FindAsync(id);
        if (param == null) return NotFound();
        _db.ProductParameters.Remove(param);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Formulas ---
    [HttpGet("formulas")]
    public async Task<IActionResult> GetFormulas([FromQuery] int? productVersionId)
    {
        var q = _db.ProductFormulas.AsQueryable();
        if (productVersionId.HasValue) q = q.Where(f => f.ProductVersionId == productVersionId.Value);
        return Ok(await q.OrderBy(f => f.ExecutionOrder).ToListAsync());
    }

    [HttpGet("formulas/{id}")]
    public async Task<IActionResult> GetFormula(int id)
    {
        var formula = await _db.ProductFormulas.FindAsync(id);
        if (formula == null) return NotFound();
        return Ok(formula);
    }

    [HttpPost("formulas")]
    public async Task<IActionResult> CreateFormula([FromBody] CreateFormulaRequest req)
    {
        var formula = new ProductFormula
        {
            ProductVersionId = req.ProductVersionId, Name = req.Name, Expression = req.Expression,
            ExecutionOrder = req.ExecutionOrder, Description = req.Description
        };
        _db.ProductFormulas.Add(formula);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetFormula), new { id = formula.Id }, formula);
    }

    [HttpPut("formulas/{id}")]
    public async Task<IActionResult> UpdateFormula(int id, [FromBody] UpdateFormulaRequest req)
    {
        var formula = await _db.ProductFormulas.FindAsync(id);
        if (formula == null) return NotFound();
        formula.Name = req.Name;
        formula.Expression = req.Expression;
        formula.ExecutionOrder = req.ExecutionOrder;
        formula.Description = req.Description;
        await _db.SaveChangesAsync();
        return Ok(formula);
    }

    [HttpDelete("formulas/{id}")]
    public async Task<IActionResult> DeleteFormula(int id)
    {
        var formula = await _db.ProductFormulas.FindAsync(id);
        if (formula == null) return NotFound();
        _db.ProductFormulas.Remove(formula);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("formulas/{id}/test")]
    public async Task<ActionResult<FormulaTestResponse>> TestFormula(int id, [FromBody] FormulaTestRequest req)
    {
        var formula = await _db.ProductFormulas.FindAsync(id);
        if (formula == null) return NotFound();
        try
        {
            var expression = string.IsNullOrWhiteSpace(req.Expression) ? formula.Expression : req.Expression;
            var result = _formulaEngine.EvaluateExpression(expression, req.Parameters);
            return Ok(new FormulaTestResponse { Result = result, Success = true });
        }
        catch (FormulaEngineException ex)
        {
            return Ok(new FormulaTestResponse { Success = false, Error = ex.Message });
        }
    }

    // --- Condition Groups ---
    [HttpGet("condition-groups")]
    public async Task<IActionResult> GetConditionGroups([FromQuery] int? productVersionId)
    {
        var q = _db.ConditionGroups.Include(cg => cg.Conditions).AsQueryable();
        if (productVersionId.HasValue) q = q.Where(cg => cg.ProductVersionId == productVersionId.Value);
        return Ok(await q.ToListAsync());
    }

    [HttpPost("condition-groups")]
    public async Task<IActionResult> CreateConditionGroup([FromBody] CreateConditionGroupRequest req)
    {
        var group = new ConditionGroup { ProductVersionId = req.ProductVersionId, Name = req.Name, LogicalOperator = req.LogicalOperator, ParentGroupId = req.ParentGroupId };
        _db.ConditionGroups.Add(group);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetConditionGroups), new { id = group.Id }, group);
    }

    // --- Conditions ---
    [HttpPost("conditions")]
    public async Task<IActionResult> CreateCondition([FromBody] CreateConditionRequest req)
    {
        var condition = new Condition { ConditionGroupId = req.ConditionGroupId, ParameterName = req.ParameterName, Operator = req.Operator, Value = req.Value, Value2 = req.Value2 };
        _db.Conditions.Add(condition);
        await _db.SaveChangesAsync();
        return Ok(condition);
    }

    // --- Insurers ---
    [HttpGet("insurers")]
    public async Task<IActionResult> GetInsurers() => Ok(await _db.Insurers.ToListAsync());

    [HttpPost("insurers")]
    public async Task<IActionResult> CreateInsurer([FromBody] Insurer insurer)
    {
        _db.Insurers.Add(insurer);
        await _db.SaveChangesAsync();
        return Ok(insurer);
    }
}
