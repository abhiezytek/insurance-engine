namespace InsuranceEngine.Api.Models;

public class Insurer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
