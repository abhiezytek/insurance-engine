namespace InsuranceEngine.Api.Models;

public class Condition
{
    public int Id { get; set; }
    public int ConditionGroupId { get; set; }
    public ConditionGroup ConditionGroup { get; set; } = null!;
    public string ParameterName { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty; // Equal, NotEqual, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual, Between, In, Contains, StartsWith, EndsWith
    public string Value { get; set; } = string.Empty;
    public string? Value2 { get; set; } // For Between operator
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
