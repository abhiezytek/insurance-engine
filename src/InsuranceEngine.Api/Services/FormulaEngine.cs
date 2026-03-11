using NCalc2;
using InsuranceEngine.Api.Models;

namespace InsuranceEngine.Api.Services;

public class FormulaEngineException : Exception
{
    public FormulaEngineException(string message, Exception? inner = null) : base(message, inner) { }
}

public class FormulaEngine
{
    public Dictionary<string, decimal> Evaluate(
        IEnumerable<ProductFormula> formulas,
        Dictionary<string, decimal> inputParameters)
    {
        var results = new Dictionary<string, decimal>(inputParameters, StringComparer.OrdinalIgnoreCase);
        var orderedFormulas = formulas.OrderBy(f => f.ExecutionOrder).ToList();

        foreach (var formula in orderedFormulas)
        {
            try
            {
                var value = EvaluateExpression(formula.Expression, results);
                results[formula.Name] = value;
            }
            catch (FormulaEngineException)
            {
                throw;
            }
            catch (DivideByZeroException ex)
            {
                throw new FormulaEngineException($"Divide by zero in formula '{formula.Name}': {formula.Expression}", ex);
            }
            catch (Exception ex)
            {
                throw new FormulaEngineException($"Error evaluating formula '{formula.Name}' ('{formula.Expression}'): {ex.Message}", ex);
            }
        }

        // Return only formula results, not input parameters
        var formulaNames = new HashSet<string>(orderedFormulas.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        return results.Where(kvp => formulaNames.Contains(kvp.Key))
                      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
    }

    public decimal EvaluateExpression(string expression, Dictionary<string, decimal> context)
    {
        var expr = new Expression(expression, EvaluateOptions.IgnoreCase);

        expr.EvaluateParameter += (name, args) =>
        {
            if (context.TryGetValue(name, out var val))
                args.Result = (double)val;
            else
                throw new FormulaEngineException($"Unknown symbol '{name}' in expression.");
        };

        expr.EvaluateFunction += (name, args) =>
        {
            var upperName = name.ToUpperInvariant();
            switch (upperName)
            {
                case "MAX":
                {
                    var values = args.Parameters.Select(p => Convert.ToDouble(p.Evaluate())).ToList();
                    args.Result = values.Max();
                    break;
                }
                case "MIN":
                {
                    var values = args.Parameters.Select(p => Convert.ToDouble(p.Evaluate())).ToList();
                    args.Result = values.Min();
                    break;
                }
                case "SUM":
                {
                    var values = args.Parameters.Select(p => Convert.ToDouble(p.Evaluate())).ToList();
                    args.Result = values.Sum();
                    break;
                }
                case "ROUND":
                {
                    var value = Convert.ToDouble(args.Parameters[0].Evaluate());
                    var decimals = args.Parameters.Length > 1 ? Convert.ToInt32(args.Parameters[1].Evaluate()) : 0;
                    args.Result = Math.Round(value, decimals);
                    break;
                }
                case "IF":
                {
                    var condition = Convert.ToBoolean(args.Parameters[0].Evaluate());
                    args.Result = condition ? args.Parameters[1].Evaluate() : args.Parameters[2].Evaluate();
                    break;
                }
                case "POWER":
                {
                    var baseVal = Convert.ToDouble(args.Parameters[0].Evaluate());
                    var exp = Convert.ToDouble(args.Parameters[1].Evaluate());
                    args.Result = Math.Pow(baseVal, exp);
                    break;
                }
            }
        };

        var result = expr.Evaluate();
        if (result == null)
            throw new FormulaEngineException($"Expression '{expression}' evaluated to null.");

        if (expr.HasErrors())
            throw new FormulaEngineException($"Invalid expression '{expression}': {expr.Error}");

        try
        {
            return Convert.ToDecimal(result);
        }
        catch (Exception ex)
        {
            throw new FormulaEngineException($"Expression '{expression}' did not return a numeric value: {ex.Message}", ex);
        }
    }
}
