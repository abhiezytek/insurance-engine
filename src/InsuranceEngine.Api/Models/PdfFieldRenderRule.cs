using System.ComponentModel.DataAnnotations;

namespace InsuranceEngine.Api.Models;

/// <summary>
/// Per-field rendering rule for PDF templates (e.g., 0.00 vs Not Applicable).
/// </summary>
public class PdfFieldRenderRule
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string TemplateCode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string FieldKey { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ProductType { get; set; }

    [MaxLength(100)]
    public string? Section { get; set; }

    [MaxLength(50)]
    public string? DataType { get; set; }

    /// <summary>NOT_APPLICABLE | ZERO | EMPTY, etc.</summary>
    [MaxLength(50)]
    public string EmptyDisplayRule { get; set; } = "ZERO";

    [MaxLength(50)]
    public string? FormatMask { get; set; }

    [MaxLength(200)]
    public string? Label { get; set; }

    [MaxLength(200)]
    public string? TemplateSource { get; set; }
}
