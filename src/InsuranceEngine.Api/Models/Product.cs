namespace InsuranceEngine.Api.Models;

public class Product
{
    public int Id { get; set; }
    public int InsurerId { get; set; }
    public Insurer Insurer { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ProductType { get; set; } = "Traditional";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ProductVersion> Versions { get; set; } = new List<ProductVersion>();
}
