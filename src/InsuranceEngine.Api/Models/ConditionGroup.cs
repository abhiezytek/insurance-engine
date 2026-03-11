namespace InsuranceEngine.Api.Models;

public class ConditionGroup
{
    public int Id { get; set; }
    public int ProductVersionId { get; set; }
    public ProductVersion ProductVersion { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string LogicalOperator { get; set; } = "AND"; // AND or OR
    public int? ParentGroupId { get; set; }
    public ConditionGroup? ParentGroup { get; set; }
    public ICollection<ConditionGroup> ChildGroups { get; set; } = new List<ConditionGroup>();
    public ICollection<Condition> Conditions { get; set; } = new List<Condition>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
