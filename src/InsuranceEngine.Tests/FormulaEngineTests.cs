using InsuranceEngine.Api.Models;
using InsuranceEngine.Api.Services;
using NUnit.Framework;
using System.Collections.Generic;

namespace InsuranceEngine.Tests;

[TestFixture]
public class FormulaEngineTests
{
    private FormulaEngine _engine = null!;

    [SetUp]
    public void SetUp() => _engine = new FormulaEngine();

    [Test]
    public void BasicArithmetic_ReturnsCorrectResult()
    {
        Assert.AreEqual(4m, _engine.EvaluateExpression("2 + 2", new Dictionary<string, decimal>()));
        Assert.AreEqual(6m, _engine.EvaluateExpression("2 * 3", new Dictionary<string, decimal>()));
        Assert.AreEqual(2m, _engine.EvaluateExpression("6 / 3", new Dictionary<string, decimal>()));
    }

    [Test]
    public void MaxFunction_ReturnsMaxValue()
    {
        var result = _engine.EvaluateExpression("MAX(3, 5, 2)", new Dictionary<string, decimal>());
        Assert.AreEqual(5m, result);
    }

    [Test]
    public void MinFunction_ReturnsMinValue()
    {
        var result = _engine.EvaluateExpression("MIN(3, 5, 2)", new Dictionary<string, decimal>());
        Assert.AreEqual(2m, result);
    }

    [Test]
    public void RoundFunction_RoundsCorrectly()
    {
        var result = _engine.EvaluateExpression("ROUND(3.567, 2)", new Dictionary<string, decimal>());
        Assert.AreEqual(3.57m, result);
    }

    [Test]
    public void PowerFunction_ReturnsCorrectPower()
    {
        var result = _engine.EvaluateExpression("POWER(2, 10)", new Dictionary<string, decimal>());
        Assert.AreEqual(1024m, result);
    }

    [Test]
    public void ParameterSubstitution_WorksCorrectly()
    {
        var ctx = new Dictionary<string, decimal> { { "AP", 10000m }, { "PPT", 10m } };
        var result = _engine.EvaluateExpression("AP * PPT", ctx);
        Assert.AreEqual(100000m, result);
    }

    [Test]
    public void GmbFormula_ReturnsCorrectValue()
    {
        var ctx = new Dictionary<string, decimal> { { "AP", 10000m } };
        var result = _engine.EvaluateExpression("AP * 11.5", ctx);
        Assert.AreEqual(115000m, result);
    }

    [Test]
    public void UnknownSymbol_ThrowsFormulaEngineException()
    {
        Assert.Throws<FormulaEngineException>(() =>
            _engine.EvaluateExpression("UNKNOWN_VAR * 2", new Dictionary<string, decimal>()));
    }

    [Test]
    public void DependencyOrdering_FormulaCanReferenceEarlierFormula()
    {
        var formulas = new List<ProductFormula>
        {
            new() { Name = "GMB", Expression = "AP * 11.5", ExecutionOrder = 1 },
            new() { Name = "GSV", Expression = "GMB * 0.30", ExecutionOrder = 2 },
        };
        var inputs = new Dictionary<string, decimal> { { "AP", 10000m } };
        var results = _engine.Evaluate(formulas, inputs);

        Assert.AreEqual(115000m, results["GMB"]);
        Assert.AreEqual(34500m, results["GSV"]);
    }

    [Test]
    public void DeathBenefitFormula_UsesMaxCorrectly()
    {
        var formulas = new List<ProductFormula>
        {
            new() { Name = "DEATH_BENEFIT", Expression = "MAX(10*AP, 1.05*TotalPremiumPaid, SurrenderValue)", ExecutionOrder = 1 },
        };
        var inputs = new Dictionary<string, decimal>
        {
            { "AP", 10000m },
            { "TotalPremiumPaid", 50000m },
            { "SurrenderValue", 40000m }
        };
        var results = _engine.Evaluate(formulas, inputs);
        // MAX(100000, 52500, 40000) = 100000
        Assert.AreEqual(100000m, results["DEATH_BENEFIT"]);
    }

    [Test]
    public void AllSampleFormulas_EvaluateCorrectly()
    {
        var formulas = new List<ProductFormula>
        {
            new() { Name = "GMB", Expression = "AP * 11.5", ExecutionOrder = 1 },
            new() { Name = "GSV", Expression = "GMB * 0.30", ExecutionOrder = 2 },
            new() { Name = "SSV", Expression = "AP * 12", ExecutionOrder = 3 },
            new() { Name = "MATURITY_BENEFIT", Expression = "GMB", ExecutionOrder = 4 },
            new() { Name = "DEATH_BENEFIT", Expression = "MAX(10*AP, 1.05*TotalPremiumPaid, SurrenderValue)", ExecutionOrder = 5 },
        };
        var inputs = new Dictionary<string, decimal>
        {
            { "AP", 10000m }, { "SA", 100000m }, { "PPT", 10m }, { "PT", 20m },
            { "Age", 35m }, { "TotalPremiumPaid", 50000m }, { "SurrenderValue", 40000m }
        };
        var results = _engine.Evaluate(formulas, inputs);
        Assert.AreEqual(115000m, results["GMB"]);
        Assert.AreEqual(34500m, results["GSV"]);
        Assert.AreEqual(120000m, results["SSV"]);
        Assert.AreEqual(115000m, results["MATURITY_BENEFIT"]);
        Assert.AreEqual(100000m, results["DEATH_BENEFIT"]);
    }
}
