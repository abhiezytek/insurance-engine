namespace InsuranceEngine.Api.Models;

public class ProductParameter
{
    public int Id { get; set; }
    public int ProductVersionId { get; set; }
    public ProductVersion ProductVersion { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = "decimal"; // decimal, string, bool, int
    public bool IsRequired { get; set; } = true;
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
