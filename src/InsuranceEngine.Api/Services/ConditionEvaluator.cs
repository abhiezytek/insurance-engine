using InsuranceEngine.Api.Models;

namespace InsuranceEngine.Api.Services;

public class ConditionEvaluator
{
    public bool Evaluate(ConditionGroup group, Dictionary<string, string> parameters)
    {
        var conditionResults = group.Conditions.Select(c => EvaluateCondition(c, parameters)).ToList();
        var childResults = group.ChildGroups.Select(cg => Evaluate(cg, parameters)).ToList();

        var allResults = conditionResults.Concat(childResults).ToList();

        if (!allResults.Any()) return true;

        return group.LogicalOperator.Equals("OR", StringComparison.OrdinalIgnoreCase)
            ? allResults.Any(r => r)
            : allResults.All(r => r);
    }

    private bool EvaluateCondition(Condition condition, Dictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue(condition.ParameterName, out var paramValue))
            return false;

        return condition.Operator switch
        {
            "Equal" => string.Equals(paramValue, condition.Value, StringComparison.OrdinalIgnoreCase),
            "NotEqual" => !string.Equals(paramValue, condition.Value, StringComparison.OrdinalIgnoreCase),
            "GreaterThan" => decimal.TryParse(paramValue, out var paramDecimal) && decimal.TryParse(condition.Value, out var conditionDecimal) && paramDecimal > conditionDecimal,
            "GreaterThanOrEqual" => decimal.TryParse(paramValue, out var paramDecimal) && decimal.TryParse(condition.Value, out var conditionDecimal) && paramDecimal >= conditionDecimal,
            "LessThan" => decimal.TryParse(paramValue, out var paramDecimal) && decimal.TryParse(condition.Value, out var conditionDecimal) && paramDecimal < conditionDecimal,
            "LessThanOrEqual" => decimal.TryParse(paramValue, out var paramDecimal) && decimal.TryParse(condition.Value, out var conditionDecimal) && paramDecimal <= conditionDecimal,
            "Between" => decimal.TryParse(paramValue, out var paramDecimal) && decimal.TryParse(condition.Value, out var lowerBound) && decimal.TryParse(condition.Value2, out var upperBound) && paramDecimal >= lowerBound && paramDecimal <= upperBound,
            "In" => condition.Value.Split(',').Select(v => v.Trim()).Contains(paramValue, StringComparer.OrdinalIgnoreCase),
            "Contains" => paramValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
            "StartsWith" => paramValue.StartsWith(condition.Value, StringComparison.OrdinalIgnoreCase),
            "EndsWith" => paramValue.EndsWith(condition.Value, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
