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

    // -----------------------------------------------------------------------
    // Factor table read/update endpoints (Admin Master UI)
    // -----------------------------------------------------------------------

    [HttpGet("factors/gmb")]
    public async Task<IActionResult> GetGmbFactors() =>
        Ok(await _db.GmbFactors.OrderBy(x => x.Ppt).ThenBy(x => x.Pt).ThenBy(x => x.EntryAgeMin).ToListAsync());

    [HttpPut("factors/gmb/{id:int}")]
    public async Task<IActionResult> UpdateGmbFactor(int id, [FromBody] GmbUpdateDto dto)
    {
        var row = await _db.GmbFactors.FindAsync(id);
        if (row is null) return NotFound();
        row.Factor = dto.Factor;
        await _db.SaveChangesAsync();
        return Ok(row);
    }

    [HttpGet("factors/gsv")]
    public async Task<IActionResult> GetGsvFactors() =>
        Ok(await _db.GsvFactors.OrderBy(x => x.Ppt).ThenBy(x => x.PolicyYear).ToListAsync());

    [HttpPut("factors/gsv/{id:int}")]
    public async Task<IActionResult> UpdateGsvFactor(int id, [FromBody] GsvUpdateDto dto)
    {
        var row = await _db.GsvFactors.FindAsync(id);
        if (row is null) return NotFound();
        row.FactorPercent = dto.FactorPercent;
        await _db.SaveChangesAsync();
        return Ok(row);
    }

    [HttpGet("factors/ssv")]
    public async Task<IActionResult> GetSsvFactors() =>
        Ok(await _db.SsvFactors.OrderBy(x => x.Ppt).ThenBy(x => x.PolicyYear).ToListAsync());

    [HttpPut("factors/ssv/{id:int}")]
    public async Task<IActionResult> UpdateSsvFactor(int id, [FromBody] SsvUpdateDto dto)
    {
        var row = await _db.SsvFactors.FindAsync(id);
        if (row is null) return NotFound();
        row.Factor1 = dto.SsvFactor1Percent;
        row.Factor2 = dto.SsvFactor2Percent;
        await _db.SaveChangesAsync();
        return Ok(row);
    }

    [HttpGet("factors/ulip-charges")]
    public async Task<IActionResult> GetUlipCharges() =>
        Ok(await _db.UlipCharges.OrderBy(x => x.ProductId).ThenBy(x => x.ChargeType).ToListAsync());

    [HttpPut("factors/ulip-charges/{id:int}")]
    public async Task<IActionResult> UpdateUlipCharge(int id, [FromBody] UlipChargeUpdateDto dto)
    {
        var row = await _db.UlipCharges.FindAsync(id);
        if (row is null) return NotFound();
        row.ChargeValue = dto.ChargeValue;
        await _db.SaveChangesAsync();
        return Ok(row);
    }

    [HttpGet("factors/mortality")]
    public async Task<IActionResult> GetMortalityRates() =>
        Ok(await _db.MortalityRates.OrderBy(x => x.Gender).ThenBy(x => x.Age).ToListAsync());

    [HttpPut("factors/mortality/{id:int}")]
    public async Task<IActionResult> UpdateMortalityRate(int id, [FromBody] MortalityUpdateDto dto)
    {
        var row = await _db.MortalityRates.FindAsync(id);
        if (row is null) return NotFound();
        row.Rate = dto.Rate;
        await _db.SaveChangesAsync();
        return Ok(row);
    }

    [HttpGet("factors/loyalty")]
    public async Task<IActionResult> GetLoyaltyFactors() =>
        Ok(await _db.LoyaltyFactors.OrderBy(x => x.Ppt).ThenBy(x => x.PolicyYearFrom).ToListAsync());

    [HttpPut("factors/loyalty/{id:int}")]
    public async Task<IActionResult> UpdateLoyaltyFactor(int id, [FromBody] LoyaltyUpdateDto dto)
    {
        var row = await _db.LoyaltyFactors.FindAsync(id);
        if (row is null) return NotFound();
        row.RatePercent = dto.RatePercent;
        await _db.SaveChangesAsync();
        return Ok(row);
    }

    // -------------------------------------------------------------------------
    // User Management
    // -------------------------------------------------------------------------

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() =>
        Ok(await _db.UserMasters.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.FullName).ToListAsync());

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var user = new UserMaster
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Mobile = dto.Mobile,
            EmployeeId = dto.EmployeeId,
            Department = dto.Department,
            PasswordHash = BCryptHash(dto.Password ?? "Welcome@123"),
            Status = dto.Status ?? "Active",
            ForceChangePassword = true,
            CreatedBy = User.Identity?.Name ?? "System"
        };
        _db.UserMasters.Add(user);
        await _db.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await _db.UserMasters.FindAsync(id);
        if (user is null) return NotFound();
        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.Email != null) user.Email = dto.Email;
        if (dto.Mobile != null) user.Mobile = dto.Mobile;
        if (dto.Department != null) user.Department = dto.Department;
        if (dto.Status != null) user.Status = dto.Status;
        await _db.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPut("users/{id:int}/toggle-status")]
    public async Task<IActionResult> ToggleUserStatus(int id)
    {
        var user = await _db.UserMasters.FindAsync(id);
        if (user is null) return NotFound();
        user.Status = user.Status == "Active" ? "Inactive" : "Active";
        await _db.SaveChangesAsync();
        return Ok(user);
    }

    // -------------------------------------------------------------------------
    // Role Management
    // -------------------------------------------------------------------------

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles() =>
        Ok(await _db.RoleMasters.Include(r => r.ModuleAccess).OrderBy(r => r.RoleName).ToListAsync());

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        var role = new RoleMaster
        {
            RoleName = dto.RoleName,
            Description = dto.Description,
            IsActive = true
        };
        _db.RoleMasters.Add(role);
        await _db.SaveChangesAsync();
        return Ok(role);
    }

    [HttpPut("roles/{id:int}")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] CreateRoleDto dto)
    {
        var role = await _db.RoleMasters.FindAsync(id);
        if (role is null) return NotFound();
        role.RoleName = dto.RoleName;
        role.Description = dto.Description;
        await _db.SaveChangesAsync();
        return Ok(role);
    }

    [HttpPut("roles/{id:int}/toggle-status")]
    public async Task<IActionResult> ToggleRoleStatus(int id)
    {
        var role = await _db.RoleMasters.FindAsync(id);
        if (role is null) return NotFound();
        role.IsActive = !role.IsActive;
        await _db.SaveChangesAsync();
        return Ok(role);
    }

    // -------------------------------------------------------------------------
    // Module & SubModule Management
    // -------------------------------------------------------------------------

    [HttpGet("modules")]
    public async Task<IActionResult> GetModules() =>
        Ok(await _db.ModuleMasters.Include(m => m.SubModules).OrderBy(m => m.ModuleName).ToListAsync());

    [HttpGet("module-access")]
    public async Task<IActionResult> GetModuleAccess([FromQuery] int? roleId) 
    {
        var query = _db.RoleModuleAccesses
            .Include(r => r.Module)
            .Include(r => r.SubModule)
            .AsQueryable();
        if (roleId.HasValue)
            query = query.Where(r => r.RoleId == roleId.Value);
        return Ok(await query.ToListAsync());
    }

    [HttpPost("module-access")]
    public async Task<IActionResult> SaveModuleAccess([FromBody] SaveModuleAccessDto dto)
    {
        // Remove existing access for this role+module
        var existing = await _db.RoleModuleAccesses
            .Where(r => r.RoleId == dto.RoleId && r.ModuleId == dto.ModuleId && r.SubModuleId == dto.SubModuleId)
            .ToListAsync();
        _db.RoleModuleAccesses.RemoveRange(existing);

        var access = new RoleModuleAccess
        {
            RoleId = dto.RoleId,
            ModuleId = dto.ModuleId,
            SubModuleId = dto.SubModuleId,
            CanView = dto.CanView,
            CanExecute = dto.CanExecute,
            CanApprove = dto.CanApprove,
            CanDownload = dto.CanDownload,
            CanUpload = dto.CanUpload,
            CanAdmin = dto.CanAdmin
        };
        _db.RoleModuleAccesses.Add(access);
        await _db.SaveChangesAsync();
        return Ok(access);
    }

    // -------------------------------------------------------------------------
    // Integration Config
    // -------------------------------------------------------------------------

    [HttpGet("integrations")]
    public async Task<IActionResult> GetIntegrations() =>
        Ok(await _db.IntegrationConfigs.OrderBy(c => c.ConfigName).ToListAsync());

    [HttpPost("integrations")]
    public async Task<IActionResult> CreateIntegration([FromBody] IntegrationConfigDto dto)
    {
        var config = new IntegrationConfig
        {
            ConfigName = dto.ConfigName,
            BaseUrl = dto.BaseUrl,
            AuthType = dto.AuthType,
            AuthToken = dto.AuthToken,
            TimeoutSeconds = dto.TimeoutSeconds,
            IsMock = dto.IsMock,
            IsActive = dto.IsActive
        };
        _db.IntegrationConfigs.Add(config);
        await _db.SaveChangesAsync();
        return Ok(config);
    }

    [HttpPut("integrations/{id:int}")]
    public async Task<IActionResult> UpdateIntegration(int id, [FromBody] IntegrationConfigDto dto)
    {
        var config = await _db.IntegrationConfigs.FindAsync(id);
        if (config is null) return NotFound();
        config.ConfigName = dto.ConfigName;
        config.BaseUrl = dto.BaseUrl;
        config.AuthType = dto.AuthType;
        config.AuthToken = dto.AuthToken;
        config.TimeoutSeconds = dto.TimeoutSeconds;
        config.IsMock = dto.IsMock;
        config.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return Ok(config);
    }

    [HttpPut("integrations/{id:int}/toggle-mock")]
    public async Task<IActionResult> ToggleMock(int id)
    {
        var config = await _db.IntegrationConfigs.FindAsync(id);
        if (config is null) return NotFound();
        config.IsMock = !config.IsMock;
        await _db.SaveChangesAsync();
        return Ok(config);
    }

    private static string BCryptHash(string password)
    {
        // Simple hash for now — in production use BCrypt.Net
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}

// DTO records for factor updates
public record GmbUpdateDto(decimal Factor);
public record GsvUpdateDto(decimal FactorPercent);
public record SsvUpdateDto(decimal SsvFactor1Percent, decimal SsvFactor2Percent);
public record UlipChargeUpdateDto(decimal ChargeValue);
public record MortalityUpdateDto(decimal Rate);
public record LoyaltyUpdateDto(decimal RatePercent);

// DTO records for admin management
public record CreateUserDto(string FullName, string Email, string? Mobile, string? EmployeeId, string? Department, string? Password, string? Status);
public record UpdateUserDto(string? FullName, string? Email, string? Mobile, string? Department, string? Status);
public record CreateRoleDto(string RoleName, string? Description);
public record SaveModuleAccessDto(int RoleId, int ModuleId, int? SubModuleId, bool CanView, bool CanExecute, bool CanApprove, bool CanDownload, bool CanUpload, bool CanAdmin);
public record IntegrationConfigDto(string ConfigName, string BaseUrl, string? AuthType, string? AuthToken, int TimeoutSeconds, bool IsMock, bool IsActive);
