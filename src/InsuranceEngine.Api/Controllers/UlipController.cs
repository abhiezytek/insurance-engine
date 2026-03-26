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
        // --- Basic input validation ---
        if (request.AnnualizedPremium <= 0)
            return BadRequest("AnnualizedPremium must be positive.");
        if (request.SumAssured < 0)
            return BadRequest("SumAssured must not be negative.");
        if (request.Ppt < 1 || request.Ppt > request.PolicyTerm)
            return BadRequest("PPT must be between 1 and PolicyTerm.");
        if (request.PolicyTerm < 5 || request.PolicyTerm > 40)
            return BadRequest("PolicyTerm must be between 5 and 40 years.");
        if (request.EntryAge <= 0 && request.DateOfBirth == default)
            return BadRequest("DateOfBirth is required.");
        if (request.EntryAge < 0 || request.EntryAge > 65)
            return BadRequest("EntryAge must be between 0 and 65 (or provide DateOfBirth).");

        // --- Option validation ---
        var allowedOptions = new[] { "Platinum", "Platinum Plus" };
        if (!string.IsNullOrWhiteSpace(request.Option) &&
            !allowedOptions.Contains(request.Option, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest($"Unsupported option '{request.Option}'. Allowed values: {string.Join(", ", allowedOptions)}.");
        }

        // --- Investment strategy validation ---
        var allowedStrategies = new[] { "Self-Managed Investment Strategy", "Age-based Investment Strategy" };
        var strategy = request.InvestmentStrategy?.Trim();
        if (!string.IsNullOrWhiteSpace(strategy) &&
            !allowedStrategies.Contains(strategy, StringComparer.OrdinalIgnoreCase) &&
            // Allow legacy aliases that are normalized downstream
            !new[] { "Self-Managed", "Life-Stage Aggressive", "Life-Stage Conservative" }
                .Contains(strategy, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest($"Unsupported investment strategy '{request.InvestmentStrategy}'. Allowed values: {string.Join(", ", allowedStrategies)}.");
        }

        // --- Premium frequency validation ---
        var allowedFrequencies = new[] { "Yearly", "Half Yearly", "HalfYearly", "Quarterly", "Monthly" };
        if (!string.IsNullOrWhiteSpace(request.PremiumFrequency) &&
            !allowedFrequencies.Contains(request.PremiumFrequency, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest($"Unsupported premium frequency '{request.PremiumFrequency}'. Allowed values: Yearly, Half Yearly, Quarterly, Monthly.");
        }

        try
        {
            var result = await _svc.CalculateAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
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
        sb.AppendLine("<title>ULIP Benefit Illustration – SUD Life e-Wealth Royale</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("@media print{.page-break{page-break-before:always}}");
        sb.AppendLine("body{font-family:Arial,sans-serif;font-size:12px;color:#222;padding:20px}");
        sb.AppendLine("h1{color:#004282;font-size:18px;margin-bottom:4px}");
        sb.AppendLine("h2{color:#004282;font-size:13px;margin-top:20px;margin-bottom:4px}");
        sb.AppendLine("table{border-collapse:collapse;width:100%;margin-top:6px}");
        sb.AppendLine("th{background:#004282;color:#fff;padding:5px 6px;text-align:right;font-size:10px;border:1px solid #003070}");
        sb.AppendLine("th:first-child{text-align:center}");
        sb.AppendLine("td{border:1px solid #ccc;padding:4px 6px;text-align:right;font-size:11px}");
        sb.AppendLine("td:first-child{text-align:center}");
        sb.AppendLine("tr:nth-child(even){background:#f5f8fc}");
        sb.AppendLine(".glance{width:auto;margin-bottom:16px}");
        sb.AppendLine(".glance td{text-align:left;padding:3px 8px}");
        sb.AppendLine(".glance td:first-child{font-weight:bold;color:#333}");
        sb.AppendLine(".disclaimer{font-size:9px;color:#555;margin-top:20px;border-top:1px solid #ccc;padding-top:10px}");
        sb.AppendLine(".note{font-size:9px;color:#555;margin-top:6px}");
        sb.AppendLine(".section-hdr{background:#e8effa;font-weight:bold;text-align:center;padding:4px;font-size:11px;border:1px solid #ccc;margin-top:10px}");
        sb.AppendLine("</style></head><body>");

        // ---- Page 1: Part A ----
        sb.AppendLine("<h1>Benefit Illustration for Linked Insurance Products – Life</h1>");
        sb.AppendLine("<h2>Policy At A Glance</h2>");
        sb.AppendLine("<table class='glance'>");
        void gl(string k, string v) => sb.AppendLine($"<tr><td>{k}</td><td>{v}</td></tr>");
        gl("Name of the Product",          "SUD Life e-Wealth Royale");
        gl("Plan Option",                  r.Option);
        gl("Customer Name",                r.CustomerName);
        gl("Entry Age",                    $"{r.EntryAge} years");
        gl("Maturity Age",                 $"{r.MaturityAge} years");
        gl("Policy Term (PT)",             $"{r.PolicyTerm} years");
        gl("Premium Payment Term (PPT)",   $"{r.Ppt} years");
        gl("Annualized Premium",           $"₹{r.AnnualizedPremium:N0}");
        gl("Premium Installment",          $"₹{r.PremiumInstallment:N0}");
        gl("Sum Assured",                  $"₹{r.SumAssured:N0}");
        gl("Premium Frequency",            r.PremiumFrequency);
        gl("Net Yield @ 4%",               $"{r.NetYield4}%");
        gl("Net Yield @ 8%",               $"{r.NetYield8}%");
        gl("GST Rate",                     "0%");
        sb.AppendLine("</table>");

        sb.AppendLine("<div class='section-hdr'>Part A &nbsp;(Amount in Rupees)</div>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead>");
        sb.AppendLine("<tr>");
        sb.AppendLine("  <th rowspan='2'>Policy<br/>Year</th>");
        sb.AppendLine("  <th rowspan='2'>Annualized<br/>Premium</th>");
        sb.AppendLine("  <th colspan='7'>At 4% p.a. Gross Investment Return</th>");
        sb.AppendLine("  <th colspan='7'>At 8% p.a. Gross Investment Return</th>");
        sb.AppendLine("</tr>");
        sb.AppendLine("<tr>");
        foreach (var _ in new[] { 0, 1 })
        {
            sb.AppendLine("  <th>Mortality<br/>Charges</th>");
            sb.AppendLine("  <th>Additional Risk Benefit<br/>Charges</th>");
            sb.AppendLine("  <th>Other<br/>Charges</th>");
            sb.AppendLine("  <th>GST</th>");
            sb.AppendLine("  <th>Fund at End<br/>of Year</th>");
            sb.AppendLine("  <th>Surrender<br/>Value</th>");
            sb.AppendLine("  <th>Death<br/>Benefit</th>");
        }
        sb.AppendLine("</tr>");
        sb.AppendLine("</thead><tbody>");
        foreach (var row in r.PartARows)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{row.Year}</td><td>₹{row.AnnualizedPremium:N0}</td>");
            sb.AppendLine($"<td>₹{row.MortalityCharges4:N0}</td><td>₹{row.ArbCharges4:N0}</td><td>₹{row.OtherCharges4:N0}</td><td>₹{row.Gst4:N0}</td>");
            sb.AppendLine($"<td>₹{row.FundAtEndOfYear4:N0}</td><td>₹{row.SurrenderValue4:N0}</td><td>₹{row.DeathBenefit4:N0}</td>");
            sb.AppendLine($"<td>₹{row.MortalityCharges8:N0}</td><td>₹{row.ArbCharges8:N0}</td><td>₹{row.OtherCharges8:N0}</td><td>₹{row.Gst8:N0}</td>");
            sb.AppendLine($"<td>₹{row.FundAtEndOfYear8:N0}</td><td>₹{row.SurrenderValue8:N0}</td><td>₹{row.DeathBenefit8:N0}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table>");
        sb.AppendLine("<p class='note'>Other Charges = Policy Administration Charges + Fund Management Charges.<br/>");
        sb.AppendLine("Surrender Value = Fund Value minus Discontinuance Charge (as per IRDAI regulations, applicable in years 1–4).</p>");

        sb.AppendLine("<div class='disclaimer'>");
        sb.AppendLine("<strong>Note:</strong> This benefit illustration is intended to show what charges are deducted from your premiums and how the unit fund, net of charges and taxes, may grow over the years of the policy term if the fund earns a gross return of 8% p.a. or 4% p.a. These rates are assumed only for the purpose of illustrating the flow of benefits. It should not be interpreted that the returns under the plan are going to be either 8% p.a. or 4% p.a.<br/><br/>");
        sb.AppendLine("<strong>IRDAI Disclosure:</strong> " + System.Net.WebUtility.HtmlEncode(r.IrdaiDisclaimer));
        sb.AppendLine("</div>");

        // ---- Page 2: Part B ----
        sb.AppendLine("<div class='page-break'></div>");
        sb.AppendLine("<h2>Part B — Detailed Charge Break-up</h2>");

        void renderPartB(List<PartBRow> rows, string rateLabel, string netYield)
        {
            sb.AppendLine($"<div class='section-hdr'>Gross yield: {rateLabel} &nbsp;|&nbsp; Net Yield: {netYield} &nbsp;(Amount in Rupees)</div>");
            sb.AppendLine("<table>");
            sb.AppendLine("<thead><tr>");
            sb.AppendLine("<th>Policy<br/>Year</th>");
            sb.AppendLine("<th>Annualized<br/>Premium</th>");
            sb.AppendLine("<th>Premium Allocation<br/>Charges</th>");
            sb.AppendLine("<th>Annualized Premium<br/>less Premium Allocation Charges</th>");
            sb.AppendLine("<th>Mortality<br/>Charges</th>");
            sb.AppendLine("<th>Additional Risk Benefit<br/>Charges</th>");
            sb.AppendLine("<th>GST</th>");
            sb.AppendLine("<th>Policy<br/>Administration Charges</th>");
            sb.AppendLine("<th>Extra Premium<br/>Allocation</th>");
            sb.AppendLine("<th>Fund Before<br/>Fund Management Charges</th>");
            sb.AppendLine("<th>Fund Management<br/>Charges</th>");
            sb.AppendLine("<th>Loyalty<br/>Addition</th>");
            sb.AppendLine("<th>Wealth<br/>Booster</th>");
            sb.AppendLine("<th>Return of Charges<br/>(combined)</th>");
            sb.AppendLine("<th>Fund at<br/>End of Year</th>");
            sb.AppendLine("<th>Surrender<br/>Value</th>");
            sb.AppendLine("<th>Death<br/>Benefit</th>");
            sb.AppendLine("</tr></thead><tbody>");
            foreach (var row in rows)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{row.Year}</td>");
                sb.AppendLine($"<td>₹{row.AnnualizedPremium:N0}</td>");
                sb.AppendLine($"<td>₹{row.PremiumAllocationCharge:N0}</td>");
                sb.AppendLine($"<td>₹{row.AnnualizedPremiumAfterPac:N0}</td>");
                sb.AppendLine($"<td>₹{row.MortalityCharges:N0}</td>");
                sb.AppendLine($"<td>₹{row.ArbCharges:N0}</td>");
                sb.AppendLine($"<td>₹{row.Gst:N0}</td>");
                sb.AppendLine($"<td>₹{row.PolicyAdministrationCharges:N0}</td>");
                sb.AppendLine($"<td>₹{row.ExtraPremiumAllocation:N0}</td>");
                sb.AppendLine($"<td>₹{row.FundBeforeFmc:N0}</td>");
                sb.AppendLine($"<td>₹{row.FundManagementCharge:N0}</td>");
                sb.AppendLine($"<td>₹{row.LoyaltyAddition:N0}</td>");
                sb.AppendLine($"<td>₹{row.WealthBooster:N0}</td>");
                sb.AppendLine($"<td>₹{row.ReturnOfCharges:N0}</td>");
                sb.AppendLine($"<td>₹{row.FundAtEndOfYear:N0}</td>");
                sb.AppendLine($"<td>₹{row.SurrenderValue:N0}</td>");
                sb.AppendLine($"<td>₹{row.DeathBenefit:N0}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody></table>");
        }

        // Net yield values are now dynamically calculated from IRR of premium cash flows vs maturity fund value
        renderPartB(r.PartBRows8, "8% p.a.", $"{r.NetYield8}%");
        sb.AppendLine("<div style='margin-top:16px'></div>");
        renderPartB(r.PartBRows4, "4% p.a.", $"{r.NetYield4}%");

        sb.AppendLine("<div class='disclaimer'>");
        sb.AppendLine("<strong>Legend:</strong> Premium Allocation Charges; Fund Management Charges (0.1118% p.m.); ");
        sb.AppendLine("Additional Risk Benefit Charges (Platinum Plus only); Loyalty Addition; Wealth Booster; ");
        sb.AppendLine("Return of Charges (combined) = Return of Policy Administration Charges (Year 10) + Return of Mortality Charges (Maturity).<br/><br/>");
        sb.AppendLine("Star Union Dai-ichi Life Insurance Company Limited | IRDAI Regn. No: 142 | UIN: 142L082V03<br/>");
        sb.AppendLine("Registered Office: 11th Floor, Vishwaroop I.T. Park, Vashi, Navi Mumbai – 400 703 | 1800 266 8833 (Toll Free)");
        sb.AppendLine("</div>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

}
