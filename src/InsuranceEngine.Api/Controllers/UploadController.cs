using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace InsuranceEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly InsuranceDbContext _db;

    public UploadController(InsuranceDbContext db) => _db = db;

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string uploadType, [FromQuery] int productVersionId)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        var allowedTypes = new[] { "Products", "Parameters", "Formulas", "Rules" };
        if (!allowedTypes.Contains(uploadType))
            return BadRequest($"uploadType must be one of: {string.Join(", ", allowedTypes)}");

        var batch = new ExcelUploadBatch { FileName = file.FileName, UploadType = uploadType, Status = "Processing" };
        _db.ExcelUploadBatches.Add(batch);
        await _db.SaveChangesAsync();

        var errors = new List<ExcelUploadRowError>();
        int processedRows = 0, errorRows = 0, totalRows = 0;

        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var stream = file.OpenReadStream();

            if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(stream);
                var lines = new List<string>();
                while (!reader.EndOfStream)
                    lines.Add(await reader.ReadLineAsync() ?? "");
                totalRows = Math.Max(0, lines.Count - 1);
                var header = lines.FirstOrDefault()?.Split(',') ?? Array.Empty<string>();
                for (int i = 1; i < lines.Count; i++)
                {
                    var row = lines[i].Split(',');
                    var (ok, err) = await ProcessRow(uploadType, header, row, productVersionId, i + 1);
                    if (ok) processedRows++; else { errorRows++; errors.Add(new ExcelUploadRowError { ExcelUploadBatchId = batch.Id, RowNumber = i + 1, ErrorMessage = err ?? "Unknown error" }); }
                }
            }
            else
            {
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return BadRequest("No worksheet found in Excel file.");
                totalRows = Math.Max(0, worksheet.Dimension?.Rows - 1 ?? 0);
                var header = Enumerable.Range(1, worksheet.Dimension?.Columns ?? 0).Select(c => worksheet.Cells[1, c].Text.Trim()).ToArray();
                for (int row = 2; row <= (worksheet.Dimension?.Rows ?? 1); row++)
                {
                    var rowData = Enumerable.Range(1, header.Length).Select(c => worksheet.Cells[row, c].Text.Trim()).ToArray();
                    var (ok, err) = await ProcessRow(uploadType, header, rowData, productVersionId, row);
                    if (ok) processedRows++; else { errorRows++; errors.Add(new ExcelUploadRowError { ExcelUploadBatchId = batch.Id, RowNumber = row, ErrorMessage = err ?? "Unknown error" }); }
                }
            }

            if (errors.Any()) _db.ExcelUploadRowErrors.AddRange(errors);
            batch.TotalRows = totalRows;
            batch.ProcessedRows = processedRows;
            batch.ErrorRows = errorRows;
            batch.Status = errorRows == 0 ? "Completed" : "CompletedWithErrors";
            batch.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { batchId = batch.Id, totalRows, processedRows, errorRows });
        }
        catch (Exception ex)
        {
            batch.Status = "Failed";
            await _db.SaveChangesAsync();
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<(bool success, string? error)> ProcessRow(string uploadType, string[] header, string[] row, int productVersionId, int rowNum)
    {
        try
        {
            var dict = header.Zip(row, (h, v) => (h, v)).ToDictionary(x => x.h, x => x.v, StringComparer.OrdinalIgnoreCase);

            switch (uploadType)
            {
                case "Parameters":
                {
                    if (!dict.TryGetValue("Name", out var name) || string.IsNullOrWhiteSpace(name))
                        return (false, "Missing 'Name' column");
                    dict.TryGetValue("DataType", out var dataType);
                    dict.TryGetValue("Description", out var desc);
                    _db.ProductParameters.Add(new ProductParameter
                    {
                        ProductVersionId = productVersionId, Name = name,
                        DataType = dataType ?? "decimal", Description = desc
                    });
                    await _db.SaveChangesAsync();
                    return (true, null);
                }
                case "Formulas":
                {
                    if (!dict.TryGetValue("Name", out var name) || string.IsNullOrWhiteSpace(name)) return (false, "Missing 'Name'");
                    if (!dict.TryGetValue("Expression", out var expr) || string.IsNullOrWhiteSpace(expr)) return (false, "Missing 'Expression'");
                    dict.TryGetValue("ExecutionOrder", out var orderStr);
                    dict.TryGetValue("Description", out var desc);
                    int.TryParse(orderStr, out var order);
                    _db.ProductFormulas.Add(new ProductFormula { ProductVersionId = productVersionId, Name = name, Expression = expr, ExecutionOrder = order, Description = desc });
                    await _db.SaveChangesAsync();
                    return (true, null);
                }
                default:
                    return (false, $"Unsupported upload type: {uploadType}");
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches()
    {
        return Ok(await _db.ExcelUploadBatches.Include(b => b.RowErrors).OrderByDescending(b => b.CreatedAt).ToListAsync());
    }
}
