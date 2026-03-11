using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Services;
using NUnit.Framework;
using System.Collections.Generic;

namespace InsuranceEngine.Tests;

[TestFixture]
public class ConditionEvaluatorTests
{
    private ConditionEvaluator _evaluator = null!;

    [SetUp]
    public void SetUp() => _evaluator = new ConditionEvaluator();

    private ConditionGroup MakeGroup(string op, params Condition[] conditions)
    {
        var group = new ConditionGroup { LogicalOperator = op, ProductVersionId = 1, Name = "test" };
        foreach (var c in conditions) group.Conditions.Add(c);
        return group;
    }

    [Test]
    public void EqualOperator_MatchesExactValue()
    {
        var group = MakeGroup("AND", new Condition { ParameterName = "Age", Operator = "Equal", Value = "30" });
        Assert.IsTrue(_evaluator.Evaluate(group, new Dictionary<string, string> { { "Age", "30" } }));
        Assert.IsFalse(_evaluator.Evaluate(group, new Dictionary<string, string> { { "Age", "31" } }));
    }

    [Test]
    public void GreaterThan_WorksCorrectly()
    {
        var group = MakeGroup("AND", new Condition { ParameterName = "Age", Operator = "GreaterThan", Value = "18" });
        Assert.IsTrue(_evaluator.Evaluate(group, new Dictionary<string, string> { { "Age", "25" } }));
        Assert.IsFalse(_evaluator.Evaluate(group, new Dictionary<string, string> { { "Age", "18" } }));
    }

    [Test]
    public void BetweenOperator_WorksCorrectly()
    {
        var group = MakeGroup("AND", new Condition { ParameterName = "Age", Operator = "Between", Value = "18", Value2 = "60" });
        Assert.IsTrue(_evaluator.Evaluate(group, new Dictionary<string, string> { { "Age", "30" } }));
        Assert.IsFalse(_evaluator.Evaluate(group, new Dictionary<string, string> { { "Age", "65" } }));
    }

    [Test]
    public void InOperator_WorksCorrectly()
    {
        var group = MakeGroup("AND", new Condition { ParameterName = "ProductType", Operator = "In", Value = "Traditional,ULIP,Term" });
        Assert.IsTrue(_evaluator.Evaluate(group, new Dictionary<string, string> { { "ProductType", "Traditional" } }));
        Assert.IsFalse(_evaluator.Evaluate(group, new Dictionary<string, string> { { "ProductType", "Other" } }));
    }

    [Test]
    public void AndGroup_AllConditionsMustMatch()
    {
        var group = MakeGroup("AND",
            new Condition { ParameterName = "Age", Operator = "GreaterThanOrEqual", Value = "18" },
            new Condition { ParameterName = "Age", Operator = "LessThanOrEqual", Value = "60" });
        Assert.IsTrue(_evaluator.Evaluate(group, new Dictionary<string, string> { { "Age", "30" } }));
        Assert.IsFalse(_evaluator.Evaluate(group, new Dictionary<string, string> { { "Age", "65" } }));
    }

    [Test]
    public void OrGroup_AnyConditionCanMatch()
    {
        var group = MakeGroup("OR",
            new Condition { ParameterName = "Type", Operator = "Equal", Value = "A" },
            new Condition { ParameterName = "Type", Operator = "Equal", Value = "B" });
        Assert.IsTrue(_evaluator.Evaluate(group, new Dictionary<string, string> { { "Type", "A" } }));
        Assert.IsTrue(_evaluator.Evaluate(group, new Dictionary<string, string> { { "Type", "B" } }));
        Assert.IsFalse(_evaluator.Evaluate(group, new Dictionary<string, string> { { "Type", "C" } }));
    }

    [Test]
    public void ContainsOperator_WorksCorrectly()
    {
        var group = MakeGroup("AND", new Condition { ParameterName = "Name", Operator = "Contains", Value = "Income" });
        Assert.IsTrue(_evaluator.Evaluate(group, new Dictionary<string, string> { { "Name", "Century Income Plan" } }));
        Assert.IsFalse(_evaluator.Evaluate(group, new Dictionary<string, string> { { "Name", "Term Plan" } }));
    }
}
