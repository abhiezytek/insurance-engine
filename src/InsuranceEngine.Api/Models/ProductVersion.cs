namespace InsuranceEngine.Api.Models;

public class ProductVersion
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Version { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ProductParameter> Parameters { get; set; } = new List<ProductParameter>();
    public ICollection<ProductFormula> Formulas { get; set; } = new List<ProductFormula>();
    public ICollection<ConditionGroup> ConditionGroups { get; set; } = new List<ConditionGroup>();
}
