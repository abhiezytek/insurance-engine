namespace InsuranceEngine.Api.Models;

public class ProductFormula
{
    public int Id { get; set; }
    public int ProductVersionId { get; set; }
    public ProductVersion ProductVersion { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public int ExecutionOrder { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
