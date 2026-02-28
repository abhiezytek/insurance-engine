using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Controllers;

/// <summary>Admin CRUD endpoints for products, versions, parameters, formulas, conditions, and insurers.</summary>
[ApiController]
[Route("api/admin")]
[Produces("application/json")]
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
    /// <summary>List all products with their insurer and versions.</summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _db.Products.Include(p => p.Insurer).Include(p => p.Versions).ToListAsync();
        return Ok(products);
    }

    /// <summary>Get a single product by ID.</summary>
    [HttpGet("products/{id}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _db.Products.Include(p => p.Insurer).Include(p => p.Versions).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    /// <summary>Create a new product.</summary>
    [HttpPost("products")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest req)
    {
        var product = new Product { InsurerId = req.InsurerId, Name = req.Name, Code = req.Code, ProductType = req.ProductType };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    // --- Versions ---
    /// <summary>List product versions, optionally filtered by productId.</summary>
    [HttpGet("versions")]
    [ProducesResponseType(typeof(IEnumerable<ProductVersion>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersions([FromQuery] int? productId)
    {
        var q = _db.ProductVersions.AsQueryable();
        if (productId.HasValue) q = q.Where(v => v.ProductId == productId.Value);
        return Ok(await q.ToListAsync());
    }

    /// <summary>Create a new product version.</summary>
    [HttpPost("versions")]
    [ProducesResponseType(typeof(ProductVersion), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateVersion([FromBody] CreateProductVersionRequest req)
    {
        var version = new ProductVersion { ProductId = req.ProductId, Version = req.Version, IsActive = req.IsActive, EffectiveDate = req.EffectiveDate };
        _db.ProductVersions.Add(version);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetVersions), new { id = version.Id }, version);
    }

    // --- Parameters ---
    /// <summary>List parameters, optionally filtered by productVersionId.</summary>
    [HttpGet("parameters")]
    [ProducesResponseType(typeof(IEnumerable<ProductParameter>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParameters([FromQuery] int? productVersionId)
    {
        var q = _db.ProductParameters.AsQueryable();
        if (productVersionId.HasValue) q = q.Where(p => p.ProductVersionId == productVersionId.Value);
        return Ok(await q.ToListAsync());
    }

    /// <summary>Create a new product parameter.</summary>
    [HttpPost("parameters")]
    [ProducesResponseType(typeof(ProductParameter), StatusCodes.Status201Created)]
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

    /// <summary>Delete a parameter by ID.</summary>
    [HttpDelete("parameters/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteParameter(int id)
    {
        var param = await _db.ProductParameters.FindAsync(id);
        if (param == null) return NotFound();
        _db.ProductParameters.Remove(param);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Formulas ---
    /// <summary>List formulas in execution order, optionally filtered by productVersionId.</summary>
    [HttpGet("formulas")]
    [ProducesResponseType(typeof(IEnumerable<ProductFormula>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFormulas([FromQuery] int? productVersionId)
    {
        var q = _db.ProductFormulas.AsQueryable();
        if (productVersionId.HasValue) q = q.Where(f => f.ProductVersionId == productVersionId.Value);
        return Ok(await q.OrderBy(f => f.ExecutionOrder).ToListAsync());
    }

    /// <summary>Get a single formula by ID.</summary>
    [HttpGet("formulas/{id}")]
    [ProducesResponseType(typeof(ProductFormula), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFormula(int id)
    {
        var formula = await _db.ProductFormulas.FindAsync(id);
        if (formula == null) return NotFound();
        return Ok(formula);
    }

    /// <summary>Create a new formula.</summary>
    [HttpPost("formulas")]
    [ProducesResponseType(typeof(ProductFormula), StatusCodes.Status201Created)]
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

    /// <summary>Update an existing formula.</summary>
    [HttpPut("formulas/{id}")]
    [ProducesResponseType(typeof(ProductFormula), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>Delete a formula by ID.</summary>
    [HttpDelete("formulas/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFormula(int id)
    {
        var formula = await _db.ProductFormulas.FindAsync(id);
        if (formula == null) return NotFound();
        _db.ProductFormulas.Remove(formula);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Test a formula expression with provided parameters.</summary>
    /// <remarks>Pass a custom <c>expression</c> to override the stored formula, or leave it empty to use the stored one.</remarks>
    [HttpPost("formulas/{id}/test")]
    [ProducesResponseType(typeof(FormulaTestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// <summary>List condition groups, optionally filtered by productVersionId.</summary>
    [HttpGet("condition-groups")]
    [ProducesResponseType(typeof(IEnumerable<ConditionGroup>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConditionGroups([FromQuery] int? productVersionId)
    {
        var q = _db.ConditionGroups.Include(cg => cg.Conditions).AsQueryable();
        if (productVersionId.HasValue) q = q.Where(cg => cg.ProductVersionId == productVersionId.Value);
        return Ok(await q.ToListAsync());
    }

    /// <summary>Create a condition group (supports nested groups via parentGroupId).</summary>
    [HttpPost("condition-groups")]
    [ProducesResponseType(typeof(ConditionGroup), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateConditionGroup([FromBody] CreateConditionGroupRequest req)
    {
        var group = new ConditionGroup { ProductVersionId = req.ProductVersionId, Name = req.Name, LogicalOperator = req.LogicalOperator, ParentGroupId = req.ParentGroupId };
        _db.ConditionGroups.Add(group);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetConditionGroups), new { id = group.Id }, group);
    }

    // --- Conditions ---
    /// <summary>Create a condition inside a condition group.</summary>
    /// <remarks>
    /// Supported operators: Equal, NotEqual, GreaterThan, GreaterThanOrEqual,
    /// LessThan, LessThanOrEqual, Between, In, Contains, StartsWith, EndsWith.
    /// Use <c>value2</c> only for the <c>Between</c> operator.
    /// </remarks>
    [HttpPost("conditions")]
    [ProducesResponseType(typeof(Condition), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCondition([FromBody] CreateConditionRequest req)
    {
        var condition = new Condition { ConditionGroupId = req.ConditionGroupId, ParameterName = req.ParameterName, Operator = req.Operator, Value = req.Value, Value2 = req.Value2 };
        _db.Conditions.Add(condition);
        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, condition);
    }

    // --- Insurers ---
    /// <summary>List all insurers.</summary>
    [HttpGet("insurers")]
    [ProducesResponseType(typeof(IEnumerable<Insurer>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInsurers() => Ok(await _db.Insurers.ToListAsync());

    /// <summary>Create a new insurer.</summary>
    [HttpPost("insurers")]
    [ProducesResponseType(typeof(Insurer), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInsurer([FromBody] Insurer insurer)
    {
        _db.Insurers.Add(insurer);
        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, insurer);
    }
}
