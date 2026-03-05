using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Services;
using InsuranceEngine.Api.Swagger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text;

namespace InsuranceEngine.Api.Controllers;

/// <summary>
/// ULIP (Unit Linked Insurance Plan) — Benefit Illustration endpoints.
/// Supports e-Wealth Royale and future ULIP products configured via database tables.
/// </summary>
[ApiController]
[Route("api/ulip")]
[Produces("application/json")]
public class UlipController : ControllerBase
{
    private readonly IUlipCalculationService _svc;
    private readonly InsuranceDbContext _db;

    public UlipController(IUlipCalculationService svc, InsuranceDbContext db)
    {
        _svc = svc;
        _db  = db;
    }

    // -----------------------------------------------------------------------
    // GET /api/ulip/products
    // -----------------------------------------------------------------------

    /// <summary>List all active ULIP products available for illustration.</summary>
    [HttpGet("products")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<object>>> GetProducts()
    {
        var products = await _db.Products
            .Where(p => p.ProductType == "ULIP")
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.ProductType,
                InsurerId = p.InsurerId,
            })
            .ToListAsync();

        return Ok(products);
    }

    // -----------------------------------------------------------------------
    // POST /api/ulip/calculate
    // -----------------------------------------------------------------------

    /// <summary>
    /// Generate a full ULIP Benefit Illustration for both 4% and 8% assumed return scenarios.
    /// The result is persisted under the supplied policy number for later retrieval.
    /// </summary>
    /// <remarks>
    /// **Sample request (e-Wealth Royale, ₹1,00,000 AP, 10PT, 10PPT, age 35):**
    /// ```json
    /// {
    ///   "policyNumber": "UL-2024-0001",
    ///   "customerName": "Rahul Sharma",
    ///   "productCode": "EWEALTH-ROYALE",
    ///   "gender": "Male",
    ///   "dateOfBirth": "1989-03-05",
    ///   "entryAge": 35,
    ///   "policyTerm": 10,
    ///   "ppt": 10,
    ///   "annualizedPremium": 100000,
    ///   "sumAssured": 1000000,
    ///   "premiumFrequency": "Yearly",
    ///   "fundAllocations": [
    ///     { "fundType": "Equity Growth Fund", "allocationPercent": 70 },
    ///     { "fundType": "Debt Fund", "allocationPercent": 30 }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(UlipCalculationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UlipCalculationResponse>> Calculate([FromBody] UlipCalculationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PolicyNumber))
            return BadRequest("PolicyNumber is required.");
        if (request.AnnualizedPremium <= 0)
            return BadRequest("AnnualizedPremium must be positive.");
        if (request.SumAssured <= 0)
            return BadRequest("SumAssured must be positive.");
        if (request.Ppt < 1 || request.Ppt > request.PolicyTerm)
            return BadRequest("PPT must be between 1 and PolicyTerm.");
        if (request.PolicyTerm < 5 || request.PolicyTerm > 30)
            return BadRequest("PolicyTerm must be between 5 and 30 years.");
        if (request.EntryAge < 0 || request.EntryAge > 65)
            return BadRequest("EntryAge must be between 0 and 65.");

        // Validate fund allocations sum to 100 when provided
        if (request.FundAllocations.Count > 0)
        {
            var total = request.FundAllocations.Sum(f => f.AllocationPercent);
            if (Math.Abs(total - 100m) > 0.01m)
                return BadRequest($"Fund allocations must sum to 100%. Current sum: {total}%.");
        }

        var result = await _svc.CalculateAsync(request);
        return Ok(result);
    }

    // -----------------------------------------------------------------------
    // GET /api/ulip/illustration/{policyNumber}
    // -----------------------------------------------------------------------

    /// <summary>Retrieve a previously computed ULIP illustration by policy number.</summary>
    [HttpGet("illustration/{policyNumber}")]
    [ProducesResponseType(typeof(UlipCalculationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UlipCalculationResponse>> GetIllustration(string policyNumber)
    {
        var result = await _svc.GetByPolicyNumberAsync(policyNumber);
        if (result == null)
            return NotFound($"No illustration found for policy number '{policyNumber}'.");
        return Ok(result);
    }

    // -----------------------------------------------------------------------
    // GET /api/ulip/pdf/{policyNumber}
    // -----------------------------------------------------------------------

    /// <summary>
    /// Generate and download the ULIP Benefit Illustration as an HTML document
    /// (printable as PDF from the browser).
    /// </summary>
    [HttpGet("pdf/{policyNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPdf(string policyNumber)
    {
        var result = await _svc.GetByPolicyNumberAsync(policyNumber);
        if (result == null)
            return NotFound($"No illustration found for policy number '{policyNumber}'.");

        var html = BuildIllustrationHtml(result);
        var bytes = Encoding.UTF8.GetBytes(html);
        return File(bytes, "text/html", $"ULIP_BI_{policyNumber}.html");
    }

    // -----------------------------------------------------------------------
    // POST /api/ulip/upload-mortality
    // -----------------------------------------------------------------------

    /// <summary>
    /// Upload a mortality rate table from an Excel (.xlsx) or CSV (.csv) file.
    ///
    /// Expected columns: Age, Rate (per 1000), Gender (optional — defaults to query param).
    /// </summary>
    [HttpPost("upload-mortality")]
    [SwaggerFileUpload]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadMortality(
        IFormFile file,
        [FromQuery] string gender = "Male",
        [FromQuery] string? effectiveDate = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        var effDate = effectiveDate != null && DateTime.TryParse(effectiveDate, out var d)
            ? d
            : DateTime.UtcNow;

        var rows = new List<MortalityRate>();
        var errors = new List<string>();

        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var lines = new List<string>();
                while (!reader.EndOfStream)
                    lines.Add(await reader.ReadLineAsync() ?? "");

                for (int i = 1; i < lines.Count; i++)
                {
                    var cols = lines[i].Split(',');
                    if (cols.Length < 2) { errors.Add($"Row {i + 1}: insufficient columns."); continue; }
                    if (!int.TryParse(cols[0].Trim(), out int age)) { errors.Add($"Row {i + 1}: invalid Age."); continue; }
                    if (!decimal.TryParse(cols[1].Trim(), out decimal rate)) { errors.Add($"Row {i + 1}: invalid Rate."); continue; }
                    var rowGender = cols.Length >= 3 && !string.IsNullOrWhiteSpace(cols[2].Trim()) ? cols[2].Trim() : gender;
                    rows.Add(new MortalityRate { Age = age, Rate = rate, Gender = rowGender, EffectiveDate = effDate });
                }
            }
            else
            {
                using var pkg = new ExcelPackage(file.OpenReadStream());
                var ws = pkg.Workbook.Worksheets[0];
                for (int r = 2; r <= ws.Dimension.Rows; r++)
                {
                    var ageCell = ws.Cells[r, 1].Text?.Trim();
                    var rateCell = ws.Cells[r, 2].Text?.Trim();
                    var genderCell = ws.Cells[r, 3].Text?.Trim();

                    if (!int.TryParse(ageCell, out int age)) { errors.Add($"Row {r}: invalid Age."); continue; }
                    if (!decimal.TryParse(rateCell, out decimal rate)) { errors.Add($"Row {r}: invalid Rate."); continue; }
                    var rowGender = !string.IsNullOrWhiteSpace(genderCell) ? genderCell : gender;
                    rows.Add(new MortalityRate { Age = age, Rate = rate, Gender = rowGender, EffectiveDate = effDate });
                }
            }
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to parse file: {ex.Message}");
        }

        if (rows.Count == 0)
            return BadRequest("No valid rows found in the file.");

        _db.MortalityRates.AddRange(rows);
        await _db.SaveChangesAsync();

        return Ok(new { processed = rows.Count, errors });
    }

    // -----------------------------------------------------------------------
    // POST /api/ulip/upload-charges
    // -----------------------------------------------------------------------

    /// <summary>
    /// Upload a ULIP charge structure from an Excel (.xlsx) or CSV (.csv) file.
    ///
    /// Expected columns: ChargeType, ChargeValue, ChargeFrequency, PolicyYear (optional).
    /// ChargeType values: PremiumAllocation, PolicyAdmin, FMC.
    /// </summary>
    [HttpPost("upload-charges")]
    [SwaggerFileUpload]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadCharges(
        IFormFile file,
        [FromQuery] string productCode = "EWEALTH-ROYALE")
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Code == productCode && p.ProductType == "ULIP");
        if (product == null)
            return BadRequest($"ULIP product with code '{productCode}' not found.");

        var rows = new List<UlipCharge>();
        var errors = new List<string>();

        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var lines = new List<string>();
                while (!reader.EndOfStream)
                    lines.Add(await reader.ReadLineAsync() ?? "");

                for (int i = 1; i < lines.Count; i++)
                {
                    var cols = lines[i].Split(',');
                    if (cols.Length < 3) { errors.Add($"Row {i + 1}: insufficient columns."); continue; }
                    var chargeType = cols[0].Trim();
                    if (!decimal.TryParse(cols[1].Trim(), out decimal val)) { errors.Add($"Row {i + 1}: invalid ChargeValue."); continue; }
                    var freq = cols[2].Trim();
                    int? py = cols.Length >= 4 && int.TryParse(cols[3].Trim(), out int pyv) ? pyv : null;
                    rows.Add(new UlipCharge { ProductId = product.Id, ChargeType = chargeType, ChargeValue = val, ChargeFrequency = freq, PolicyYear = py });
                }
            }
            else
            {
                using var pkg = new ExcelPackage(file.OpenReadStream());
                var ws = pkg.Workbook.Worksheets[0];
                for (int r = 2; r <= ws.Dimension.Rows; r++)
                {
                    var chargeType = ws.Cells[r, 1].Text?.Trim() ?? string.Empty;
                    if (!decimal.TryParse(ws.Cells[r, 2].Text?.Trim(), out decimal val)) { errors.Add($"Row {r}: invalid ChargeValue."); continue; }
                    var freq = ws.Cells[r, 3].Text?.Trim() ?? string.Empty;
                    int? py = int.TryParse(ws.Cells[r, 4].Text?.Trim(), out int pyv) ? pyv : null;
                    rows.Add(new UlipCharge { ProductId = product.Id, ChargeType = chargeType, ChargeValue = val, ChargeFrequency = freq, PolicyYear = py });
                }
            }
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to parse file: {ex.Message}");
        }

        if (rows.Count == 0)
            return BadRequest("No valid rows found in the file.");

        // Replace existing charges for this product
        var existing = _db.UlipCharges.Where(c => c.ProductId == product.Id);
        _db.UlipCharges.RemoveRange(existing);
        _db.UlipCharges.AddRange(rows);
        await _db.SaveChangesAsync();

        return Ok(new { processed = rows.Count, errors });
    }

    // -----------------------------------------------------------------------
    // HTML illustration builder (used for "PDF" download)
    // -----------------------------------------------------------------------

    private static string BuildIllustrationHtml(UlipCalculationResponse r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/>");
        sb.AppendLine("<title>ULIP Benefit Illustration</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Arial,sans-serif;font-size:13px;color:#222;padding:24px}");
        sb.AppendLine("h1{color:#004282;font-size:20px}h2{color:#004282;font-size:15px;margin-top:24px}");
        sb.AppendLine("table{border-collapse:collapse;width:100%;margin-top:8px}");
        sb.AppendLine("th{background:#004282;color:#fff;padding:6px 8px;text-align:right;font-size:11px}");
        sb.AppendLine("th:first-child{text-align:center}");
        sb.AppendLine("td{border:1px solid #ddd;padding:5px 8px;text-align:right;font-size:12px}");
        sb.AppendLine("td:first-child{text-align:center}");
        sb.AppendLine("tr:nth-child(even){background:#f5f8fc}");
        sb.AppendLine(".disclaimer{font-size:10px;color:#555;margin-top:24px;border-top:1px solid #ccc;padding-top:12px}");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<h1>ULIP Benefit Illustration</h1>");
        sb.AppendLine("<h2>Policy At A Glance</h2>");
        sb.AppendLine("<table style='width:auto;margin-bottom:16px'>");
        void row2(string k, string v) => sb.AppendLine($"<tr><td style='text-align:left;font-weight:bold'>{k}</td><td style='text-align:left'>{v}</td></tr>");
        row2("Policy Number",      r.PolicyNumber);
        row2("Customer Name",      r.CustomerName);
        row2("Product",            r.ProductName);
        row2("Gender",             r.Gender);
        row2("Entry Age",          $"{r.EntryAge} years");
        row2("Policy Term (PT)",   $"{r.PolicyTerm} years");
        row2("Premium Payment Term (PPT)", $"{r.Ppt} years");
        row2("Annualized Premium (AP)", $"₹{r.AnnualizedPremium:N0}");
        row2("Sum Assured (SA)",   $"₹{r.SumAssured:N0}");
        row2("Premium Frequency",  r.PremiumFrequency);
        row2("Maturity Benefit @ 4%", $"₹{r.MaturityBenefit4:N2}");
        row2("Maturity Benefit @ 8%", $"₹{r.MaturityBenefit8:N2}");
        sb.AppendLine("</table>");

        sb.AppendLine("<h2>Benefit Illustration Table</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th>Year</th><th>Age</th><th>Annual Premium (AP)</th><th>Premium Invested</th>");
        sb.AppendLine("<th>Mortality Charges (MC)</th><th>Policy Charges (PC)</th>");
        sb.AppendLine("<th>Fund Value @ 4%</th><th>Death Benefit @ 4%</th>");
        sb.AppendLine("<th>Fund Value @ 8%</th><th>Death Benefit @ 8%</th>");
        sb.AppendLine("</tr></thead><tbody>");

        foreach (var row in r.YearlyTable)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{row.Year}</td><td>{row.Age}</td>");
            sb.AppendLine($"<td>₹{row.AnnualPremium:N2}</td><td>₹{row.PremiumInvested:N2}</td>");
            sb.AppendLine($"<td>₹{row.MortalityCharge:N2}</td><td>₹{row.PolicyCharge:N2}</td>");
            sb.AppendLine($"<td>₹{row.FundValue4:N2}</td><td>₹{row.DeathBenefit4:N2}</td>");
            sb.AppendLine($"<td>₹{row.FundValue8:N2}</td><td>₹{row.DeathBenefit8:N2}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<div class='disclaimer'>");
        sb.AppendLine("<strong>IRDAI Disclaimer:</strong><br/>");
        sb.AppendLine(System.Net.WebUtility.HtmlEncode(r.IrdaiDisclaimer));
        sb.AppendLine("</div>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
