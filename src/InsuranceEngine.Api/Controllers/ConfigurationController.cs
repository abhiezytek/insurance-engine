using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Security;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace InsuranceEngine.Api.Controllers;

/// <summary>Configuration module endpoints for products, UINs, parameters, formulas, factors, and integrations.</summary>
[ApiController]
[Route("api/configuration")]
[Produces("application/json")]
[Authorize(Policy = "CanEditConfiguration")]
[RequireRoleHeader("Admin", "SuperAdmin", "Actuary")]
public class ConfigurationController : ControllerBase
{
    private readonly InsuranceDbContext _db;
    private readonly ILogger<ConfigurationController> _logger;
    private readonly IActivityAuditService _audit;

    public ConfigurationController(InsuranceDbContext db, ILogger<ConfigurationController> logger, IActivityAuditService audit)
    {
        _db = db;
        _logger = logger;
        _audit = audit;
    }

    private void LogRequest(string path, int status, int? count = null)
    {
        var role = HttpContext.User?.Claims
            .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
        _logger.LogInformation("Configuration request {Path} status={Status} role={Role} count={Count}",
            path, status, role ?? "<none>", count);
    }

    // -----------------------------------------------------------------------
    // Products & UINs
    // -----------------------------------------------------------------------

    /// <summary>List all products with type info (code, name, productType).</summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(IEnumerable<ConfigProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _db.Products
            .Select(p => new ConfigProductDto(p.Code, p.Name, p.ProductType))
            .ToListAsync();
        LogRequest(HttpContext.Request.Path, StatusCodes.Status200OK, products.Count);
        return Ok(products);
    }

    /// <summary>List UINs (product versions) mapped to a product by code.</summary>
    [HttpGet("uins")]
    [ProducesResponseType(typeof(IEnumerable<ConfigUinDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUins([FromQuery] string productCode)
    {
        var product = await _db.Products
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Code == productCode);
        if (product is null) return NotFound(new { error = $"Product '{productCode}' not found." });

        var uins = product.Versions
            .Select(v => new ConfigUinDto(v.Id, v.Version, v.IsActive, v.EffectiveDate))
            .ToList();
        LogRequest(HttpContext.Request.Path, StatusCodes.Status200OK, uins.Count);
        return Ok(uins);
    }

    // -----------------------------------------------------------------------
    // Parameters
    // -----------------------------------------------------------------------

    /// <summary>List available parameter names/descriptions for the formula editor.</summary>
    [HttpGet("parameters")]
    [ProducesResponseType(typeof(IEnumerable<ConfigParameterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParameters([FromQuery] string productCode, [FromQuery] string uin)
    {
        var versionId = await ResolveVersionIdAsync(productCode, uin);
        if (versionId is null) return NotFound(new { error = $"Product version not found for code='{productCode}', uin='{uin}'." });

        var parameters = await _db.ProductParameters
            .Where(p => p.ProductVersionId == versionId.Value)
            .Select(p => new ConfigParameterDto(p.Id, p.Name, p.DataType, p.IsRequired, p.DefaultValue, p.Description))
            .ToListAsync();
        LogRequest(HttpContext.Request.Path, StatusCodes.Status200OK, parameters.Count);
        return Ok(parameters);
    }

    // -----------------------------------------------------------------------
    // Formulas (with version history via FormulaMaster)
    // -----------------------------------------------------------------------

    /// <summary>List formulas for a product + UIN from the FormulaMaster table.</summary>
    [HttpGet("formulas")]
    [ProducesResponseType(typeof(IEnumerable<FormulaMaster>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFormulas([FromQuery] string productCode, [FromQuery] string uin)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Code == productCode);
        if (product is null) return NotFound(new { error = $"Product '{productCode}' not found." });

        var formulas = await _db.FormulaMasters
            .Where(f => f.ProductName == product.Name && f.Uin == uin)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
        LogRequest(HttpContext.Request.Path, StatusCodes.Status200OK, formulas.Count);
        return Ok(formulas);
    }

    /// <summary>Save a formula, creating a new FormulaMaster entry for version history.</summary>
    [HttpPost("formula")]
    [ProducesResponseType(typeof(FormulaMaster), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveFormula([FromBody] SaveFormulaDto dto)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Code == dto.ProductCode);
        if (product is null) return NotFound(new { error = $"Product '{dto.ProductCode}' not found." });

        // Deactivate previous active versions for the same formula key
        var previousActive = await _db.FormulaMasters
            .Where(f => f.ProductName == product.Name && f.Uin == dto.Uin && f.FormulaType == dto.FormulaKey && f.IsActive)
            .ToListAsync();
        foreach (var prev in previousActive)
        {
            prev.IsActive = false;
            prev.ExpiryDate = DateTime.UtcNow;
        }

        var formula = new FormulaMaster
        {
            Uin = dto.Uin,
            ProductName = product.Name,
            FormulaType = dto.FormulaKey,
            FormulaRuleJson = dto.FormulaExpression,
            EffectiveDate = DateTime.UtcNow,
            IsActive = true,
            CreatedBy = dto.ChangedBy,
            CreatedAt = DateTime.UtcNow
        };
        _db.FormulaMasters.Add(formula);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Formula saved: ProductCode={ProductCode} UIN={Uin} Key={Key} by {User}",
            dto.ProductCode, dto.Uin, dto.FormulaKey, dto.ChangedBy);

        var previousExpression = previousActive.FirstOrDefault()?.FormulaRuleJson;
        await _audit.LogAsync("Configuration", "FormulaUpdated",
            recordId: $"{dto.ProductCode}/{dto.Uin}/{dto.FormulaKey}",
            oldValue: previousExpression,
            newValue: dto.FormulaExpression);

        return CreatedAtAction(nameof(GetFormulaHistory), new { productCode = dto.ProductCode, uin = dto.Uin, formulaKey = dto.FormulaKey }, formula);
    }

    /// <summary>Get version history for a specific formula key.</summary>
    [HttpGet("formula-history")]
    [ProducesResponseType(typeof(IEnumerable<FormulaMaster>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFormulaHistory([FromQuery] string productCode, [FromQuery] string uin, [FromQuery] string formulaKey)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Code == productCode);
        if (product is null) return NotFound(new { error = $"Product '{productCode}' not found." });

        var history = await _db.FormulaMasters
            .Where(f => f.ProductName == product.Name && f.Uin == uin && f.FormulaType == formulaKey)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
        LogRequest(HttpContext.Request.Path, StatusCodes.Status200OK, history.Count);
        return Ok(history);
    }

    /// <summary>Restore a previous formula version by creating a new active entry from a historical one.</summary>
    [HttpPost("formula/{id:int}/restore")]
    [ProducesResponseType(typeof(FormulaMaster), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreFormula(int id, [FromBody] RestoreFormulaDto? dto = null)
    {
        var source = await _db.FormulaMasters.FindAsync(id);
        if (source is null) return NotFound(new { error = $"Formula version with id={id} not found." });

        // Deactivate current active versions for the same formula key
        var currentActive = await _db.FormulaMasters
            .Where(f => f.ProductName == source.ProductName && f.Uin == source.Uin && f.FormulaType == source.FormulaType && f.IsActive)
            .ToListAsync();
        foreach (var current in currentActive)
        {
            current.IsActive = false;
            current.ExpiryDate = DateTime.UtcNow;
        }

        var restored = new FormulaMaster
        {
            Uin = source.Uin,
            ProductName = source.ProductName,
            FormulaType = source.FormulaType,
            FormulaRuleJson = source.FormulaRuleJson,
            EffectiveDate = DateTime.UtcNow,
            IsActive = true,
            CreatedBy = dto?.RestoredBy ?? $"restored-from-{source.Id}",
            CreatedAt = DateTime.UtcNow
        };
        _db.FormulaMasters.Add(restored);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Formula restored from id={SourceId} to new id={NewId}", source.Id, restored.Id);

        var previousExpression = currentActive.FirstOrDefault()?.FormulaRuleJson;
        await _audit.LogAsync("Configuration", "FormulaRestored",
            recordId: $"{source.ProductName}/{source.Uin}/{source.FormulaType}",
            oldValue: previousExpression,
            newValue: source.FormulaRuleJson);

        return CreatedAtAction(nameof(GetFormulaHistory),
            new { productCode = string.Empty, uin = source.Uin, formulaKey = source.FormulaType }, restored);
    }

    // -----------------------------------------------------------------------
    // Formula Validation
    // -----------------------------------------------------------------------

    /// <summary>Validate a formula expression for syntax correctness and valid parameter references.</summary>
    [HttpPost("formula/validate")]
    [ProducesResponseType(typeof(FormulaValidationResult), StatusCodes.Status200OK)]
    public IActionResult ValidateFormula([FromBody] FormulaValidateDto dto)
    {
        var errors = new List<string>();
        var expression = dto.FormulaExpression ?? string.Empty;
        var allowedParams = new HashSet<string>(dto.ParameterNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(expression))
        {
            errors.Add("Formula expression cannot be empty.");
            return Ok(new FormulaValidationResult(false, errors));
        }

        // 1. Balanced parentheses check
        ValidateBalancedParentheses(expression, errors);

        // 2. Balanced square brackets check
        ValidateBalancedBrackets(expression, errors);

        // 3. Invalid character sequences
        ValidateNoInvalidSequences(expression, errors);

        // 4. Check for empty parentheses
        if (Regex.IsMatch(expression, @"\(\s*\)"))
            errors.Add("Formula contains empty parentheses '()'.");

        // 5. Check for consecutive operators (allow unary minus after another operator)
        if (Regex.IsMatch(expression, @"[+\-*/]\s*[+*/]"))
            errors.Add("Formula contains consecutive operators (e.g. '++', '*/') which is invalid.");

        // 6. Check for operators at start/end (excluding unary minus)
        var trimmed = expression.Trim();
        if (Regex.IsMatch(trimmed, @"^[+*/]"))
            errors.Add("Formula cannot start with an operator (+, *, /).");
        if (Regex.IsMatch(trimmed, @"[+\-*/]$"))
            errors.Add("Formula cannot end with an operator.");

        // 7. Check for operator adjacent to parenthesis issues
        if (Regex.IsMatch(expression, @"\(\s*[+*/]"))
            errors.Add("Formula has an operator immediately after an opening parenthesis.");
        if (Regex.IsMatch(expression, @"[+\-*/]\s*\)"))
            errors.Add("Formula has an operator immediately before a closing parenthesis.");

        // 8. Validate parameter references
        if (allowedParams.Count > 0)
        {
            ValidateParameterReferences(expression, allowedParams, errors);
        }

        // 9. Check for assignment operators (common mistake) — exclude ==, !=, <=, >=
        if (Regex.IsMatch(expression, @"(?<![!<>=])=(?!=)"))
            errors.Add("Formula contains an assignment operator '='. Use '==' for equality comparison.");

        return Ok(new FormulaValidationResult(errors.Count == 0, errors));
    }

    private static void ValidateBalancedParentheses(string expression, List<string> errors)
    {
        int depth = 0;
        int position = 0;
        foreach (char c in expression)
        {
            position++;
            if (c == '(') depth++;
            else if (c == ')')
            {
                depth--;
                if (depth < 0)
                {
                    errors.Add($"Unexpected closing parenthesis ')' at position {position}.");
                    return;
                }
            }
        }
        if (depth > 0)
            errors.Add($"Missing {depth} closing parenthesis(es) ')'.");
    }

    private static void ValidateBalancedBrackets(string expression, List<string> errors)
    {
        int depth = 0;
        int position = 0;
        foreach (char c in expression)
        {
            position++;
            if (c == '[') depth++;
            else if (c == ']')
            {
                depth--;
                if (depth < 0)
                {
                    errors.Add($"Unexpected closing bracket ']' at position {position}.");
                    return;
                }
            }
        }
        if (depth > 0)
            errors.Add($"Missing {depth} closing bracket(s) ']'.");
    }

    private static void ValidateNoInvalidSequences(string expression, List<string> errors)
    {
        // Check for double dots (invalid decimal)
        if (expression.Contains(".."))
            errors.Add("Formula contains '..' which is not a valid number format.");

        // Check for misplaced commas
        if (Regex.IsMatch(expression, @",,"))
            errors.Add("Formula contains consecutive commas.");

        // Check for semicolons (not typically valid in formula expressions)
        if (expression.Contains(';'))
            errors.Add("Formula contains a semicolon ';' which is not valid in formula expressions.");
    }

    private static void ValidateParameterReferences(string expression, HashSet<string> allowedParams, List<string> errors)
    {
        // Extract identifiers: word sequences that are not purely numeric and not common functions/keywords
        var knownFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MIN", "MAX", "ABS", "ROUND", "FLOOR", "CEILING", "CEIL", "SQRT", "POW", "LOG",
            "SUM", "AVG", "COUNT", "IF", "AND", "OR", "NOT", "TRUE", "FALSE",
            "Math", "Convert", "Decimal", "Double", "Int32"
        };

        var identifierPattern = new Regex(@"[A-Za-z_][A-Za-z0-9_]*");
        var matches = identifierPattern.Matches(expression);
        var unknownParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in matches)
        {
            var identifier = match.Value;
            if (!knownFunctions.Contains(identifier) && !allowedParams.Contains(identifier))
            {
                unknownParams.Add(identifier);
            }
        }

        foreach (var unknown in unknownParams)
        {
            errors.Add($"Unknown parameter reference '{unknown}'. Allowed parameters: {string.Join(", ", allowedParams)}.");
        }
    }

    // -----------------------------------------------------------------------
    // Factors (GET with product scope)
    // -----------------------------------------------------------------------

    /// <summary>Get GMB factors, optionally filtered by product code.</summary>
    [HttpGet("factors/gmb")]
    [ProducesResponseType(typeof(IEnumerable<GmbFactor>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGmbFactors([FromQuery] string? productCode)
    {
        var rows = await _db.GmbFactors.OrderBy(x => x.Ppt).ThenBy(x => x.Pt).ThenBy(x => x.EntryAgeMin).ToListAsync();
        LogRequest(HttpContext.Request.Path, StatusCodes.Status200OK, rows.Count);
        return Ok(rows);
    }

    /// <summary>Get GSV factors, optionally filtered by product code.</summary>
    [HttpGet("factors/gsv")]
    [ProducesResponseType(typeof(IEnumerable<GsvFactor>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGsvFactors([FromQuery] string? productCode)
    {
        var rows = await _db.GsvFactors.OrderBy(x => x.Ppt).ThenBy(x => x.PolicyYear).ToListAsync();
        LogRequest(HttpContext.Request.Path, StatusCodes.Status200OK, rows.Count);
        return Ok(rows);
    }

    /// <summary>Get SSV factors, optionally filtered by product code.</summary>
    [HttpGet("factors/ssv")]
    [ProducesResponseType(typeof(IEnumerable<SsvFactor>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSsvFactors([FromQuery] string? productCode)
    {
        var rows = await _db.SsvFactors.OrderBy(x => x.Ppt).ThenBy(x => x.PolicyYear).ToListAsync();
        LogRequest(HttpContext.Request.Path, StatusCodes.Status200OK, rows.Count);
        return Ok(rows);
    }

    /// <summary>Get mortality rates, optionally filtered by product code.</summary>
    [HttpGet("factors/mortality")]
    [ProducesResponseType(typeof(IEnumerable<MortalityRate>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMortalityRates([FromQuery] string? productCode)
    {
        var rows = await _db.MortalityRates.OrderBy(x => x.Gender).ThenBy(x => x.Age).ToListAsync();
        LogRequest(HttpContext.Request.Path, StatusCodes.Status200OK, rows.Count);
        return Ok(rows);
    }

    /// <summary>Get loyalty factors, optionally filtered by product code.</summary>
    [HttpGet("factors/loyalty")]
    [ProducesResponseType(typeof(IEnumerable<LoyaltyFactor>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLoyaltyFactors([FromQuery] string? productCode)
    {
        var rows = await _db.LoyaltyFactors.OrderBy(x => x.Ppt).ThenBy(x => x.PolicyYearFrom).ToListAsync();
        LogRequest(HttpContext.Request.Path, StatusCodes.Status200OK, rows.Count);
        return Ok(rows);
    }

    /// <summary>Get ULIP charges, optionally filtered by product code.</summary>
    [HttpGet("factors/ulip-charges")]
    [ProducesResponseType(typeof(IEnumerable<UlipCharge>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUlipCharges([FromQuery] string? productCode)
    {
        var query = _db.UlipCharges.AsQueryable();
        if (!string.IsNullOrWhiteSpace(productCode))
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Code == productCode);
            if (product is not null)
                query = query.Where(x => x.ProductId == product.Id);
        }
        var rows = await query.OrderBy(x => x.ProductId).ThenBy(x => x.ChargeType).ToListAsync();
        LogRequest(HttpContext.Request.Path, StatusCodes.Status200OK, rows.Count);
        return Ok(rows);
    }

    /// <summary>Bulk update factor rows.</summary>
    [HttpPut("factors")]
    [ProducesResponseType(typeof(BulkFactorUpdateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUpdateFactors([FromBody] BulkFactorUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FactorType))
            return BadRequest(new { error = "factorType is required." });

        int updatedCount = 0;
        var errors = new List<string>();

        switch (dto.FactorType.ToLowerInvariant())
        {
            case "gmb":
                foreach (var row in dto.Rows)
                {
                    if (!row.TryGetValue("id", out var idObj)) continue;
                    var id = Convert.ToInt32(idObj);
                    var entity = await _db.GmbFactors.FindAsync(id);
                    if (entity is null) { errors.Add($"GMB factor id={id} not found."); continue; }
                    if (row.TryGetValue("factor", out var factorVal))
                        entity.Factor = Convert.ToDecimal(factorVal);
                    updatedCount++;
                }
                break;

            case "gsv":
                foreach (var row in dto.Rows)
                {
                    if (!row.TryGetValue("id", out var idObj)) continue;
                    var id = Convert.ToInt32(idObj);
                    var entity = await _db.GsvFactors.FindAsync(id);
                    if (entity is null) { errors.Add($"GSV factor id={id} not found."); continue; }
                    if (row.TryGetValue("factorPercent", out var fpVal))
                        entity.FactorPercent = Convert.ToDecimal(fpVal);
                    updatedCount++;
                }
                break;

            case "ssv":
                foreach (var row in dto.Rows)
                {
                    if (!row.TryGetValue("id", out var idObj)) continue;
                    var id = Convert.ToInt32(idObj);
                    var entity = await _db.SsvFactors.FindAsync(id);
                    if (entity is null) { errors.Add($"SSV factor id={id} not found."); continue; }
                    if (row.TryGetValue("factor1", out var f1Val))
                        entity.Factor1 = Convert.ToDecimal(f1Val);
                    if (row.TryGetValue("factor2", out var f2Val))
                        entity.Factor2 = Convert.ToDecimal(f2Val);
                    updatedCount++;
                }
                break;

            case "mortality":
                foreach (var row in dto.Rows)
                {
                    if (!row.TryGetValue("id", out var idObj)) continue;
                    var id = Convert.ToInt32(idObj);
                    var entity = await _db.MortalityRates.FindAsync(id);
                    if (entity is null) { errors.Add($"Mortality rate id={id} not found."); continue; }
                    if (row.TryGetValue("rate", out var rateVal))
                        entity.Rate = Convert.ToDecimal(rateVal);
                    updatedCount++;
                }
                break;

            case "loyalty":
                foreach (var row in dto.Rows)
                {
                    if (!row.TryGetValue("id", out var idObj)) continue;
                    var id = Convert.ToInt32(idObj);
                    var entity = await _db.LoyaltyFactors.FindAsync(id);
                    if (entity is null) { errors.Add($"Loyalty factor id={id} not found."); continue; }
                    if (row.TryGetValue("ratePercent", out var rpVal))
                        entity.RatePercent = Convert.ToDecimal(rpVal);
                    updatedCount++;
                }
                break;

            case "ulip-charges":
                foreach (var row in dto.Rows)
                {
                    if (!row.TryGetValue("id", out var idObj)) continue;
                    var id = Convert.ToInt32(idObj);
                    var entity = await _db.UlipCharges.FindAsync(id);
                    if (entity is null) { errors.Add($"ULIP charge id={id} not found."); continue; }
                    if (row.TryGetValue("chargeValue", out var cvVal))
                        entity.ChargeValue = Convert.ToDecimal(cvVal);
                    updatedCount++;
                }
                break;

            default:
                return BadRequest(new { error = $"Unknown factor type '{dto.FactorType}'." });
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Bulk factor update: type={FactorType} productCode={ProductCode} uin={Uin} updated={Count} errors={Errors}",
            dto.FactorType, dto.ProductCode, dto.Uin, updatedCount, errors.Count);

        await _audit.LogAsync("Configuration", "FactorUpdated",
            recordId: $"{dto.FactorType}/{dto.ProductCode}/{dto.Uin}",
            newValue: $"Updated {updatedCount} rows");

        return Ok(new BulkFactorUpdateResult(updatedCount, errors));
    }

    // -----------------------------------------------------------------------
    // Integration Config
    // -----------------------------------------------------------------------

    /// <summary>List all integration configurations.</summary>
    [HttpGet("integrations")]
    [ProducesResponseType(typeof(IEnumerable<IntegrationConfig>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIntegrations()
    {
        var configs = await _db.IntegrationConfigs.OrderBy(c => c.ConfigName).ToListAsync();
        LogRequest(HttpContext.Request.Path, StatusCodes.Status200OK, configs.Count);
        return Ok(configs);
    }

    /// <summary>Update an integration configuration by ID.</summary>
    [HttpPut("integrations/{id:int}")]
    [ProducesResponseType(typeof(IntegrationConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIntegration(int id, [FromBody] ConfigIntegrationUpdateDto dto)
    {
        var config = await _db.IntegrationConfigs.FindAsync(id);
        if (config is null) return NotFound(new { error = $"Integration config id={id} not found." });

        config.ConfigName = dto.ConfigName;
        config.BaseUrl = dto.BaseUrl;
        config.AuthType = dto.AuthType;
        config.AuthToken = dto.AuthToken;
        config.TimeoutSeconds = dto.TimeoutSeconds;
        config.IsMock = dto.IsMock;
        config.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Integration config updated: id={Id} name={Name}", id, dto.ConfigName);

        await _audit.LogAsync("Configuration", "IntegrationConfigUpdated",
            recordId: id.ToString(),
            newValue: dto.ConfigName);

        return Ok(config);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private async Task<int?> ResolveVersionIdAsync(string productCode, string uin)
    {
        var version = await _db.ProductVersions
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Product.Code == productCode && v.Version == uin);
        return version?.Id;
    }
}

// -----------------------------------------------------------------------
// DTO records for Configuration endpoints
// -----------------------------------------------------------------------

public record ConfigProductDto(string Code, string Name, string ProductType);
public record ConfigUinDto(int Id, string Uin, bool IsActive, DateTime EffectiveDate);
public record ConfigParameterDto(int Id, string Name, string DataType, bool IsRequired, string? DefaultValue, string? Description);

public record SaveFormulaDto(string ProductCode, string Uin, string FormulaKey, string FormulaExpression, string ChangedBy);
public record RestoreFormulaDto(string? RestoredBy);

public record FormulaValidateDto(string FormulaExpression, string[]? ParameterNames);
public record FormulaValidationResult(bool IsValid, List<string> Errors);

public record BulkFactorUpdateDto(string ProductCode, string Uin, string FactorType, List<Dictionary<string, object>> Rows);
public record BulkFactorUpdateResult(int UpdatedCount, List<string> Errors);

public record ConfigIntegrationUpdateDto(string ConfigName, string BaseUrl, string? AuthType, string? AuthToken, int TimeoutSeconds, bool IsMock, bool IsActive);
