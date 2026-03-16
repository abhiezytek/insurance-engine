namespace InsuranceEngine.Api.Models;

public class FormulaMaster
{
    public int Id { get; set; }
    public string Uin { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string FormulaType { get; set; } = string.Empty;
    public string FormulaRuleJson { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
