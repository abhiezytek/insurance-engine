using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.DTOs;
using System.Threading;

namespace InsuranceEngine.Api.Services;

internal record RiskPreferenceRule(string InvestmentStrategy, string RiskPreference, bool IsRequired);

/// <summary>
/// Lightweight CSV-backed rule loader for investment strategy ↔ risk preference behavior.
/// Keeps UI and backend validation table-driven as per ewealth_risk_preference_rules.csv.
/// </summary>
internal static class RiskPreferenceRuleBook
{
    private static readonly Lazy<IReadOnlyList<RiskPreferenceRule>> CachedRules = new(LoadRules, LazyThreadSafetyMode.ExecutionAndPublication);

    public static IReadOnlyList<RiskPreferenceRule> Rules => CachedRules.Value;

    public static (bool IsValid, string? Error) ValidateAndNormalize(UlipCalculationRequest req, Func<bool> hasAgeBasedAllocationMaster)
    {
        req.FundAllocations ??= new List<UlipFundAllocation>();
        NormalizeAliases(req);

        var rules = Rules;
        if (rules.Count == 0)
            return (false, "Risk preference rules are not configured. Ensure ewealth_risk_preference_rules.csv is present in the docs directory and restart the service after updating.");

        var strategy = req.InvestmentStrategy?.Trim();
        if (string.IsNullOrWhiteSpace(strategy))
            return (false, "InvestmentStrategy is required.");

        var strategyRules = rules.Where(r => r.InvestmentStrategy.Equals(strategy, StringComparison.OrdinalIgnoreCase)).ToList();
        if (strategyRules.Count == 0)
            return (false, $"Investment strategy '{req.InvestmentStrategy}' is not supported by the risk preference rules.");

        var isAgeBased = strategyRules.Any(r => r.InvestmentStrategy.Equals("Age-based Investment Strategy", StringComparison.OrdinalIgnoreCase));
        if (isAgeBased)
        {
            if (string.IsNullOrWhiteSpace(req.RiskPreference))
                return (false, "RiskPreference is required when InvestmentStrategy = Age-based Investment Strategy.");

            if (!strategyRules.Any(r => r.RiskPreference.Equals(req.RiskPreference, StringComparison.OrdinalIgnoreCase)))
                return (false, $"RiskPreference '{req.RiskPreference}' is not allowed for Age-based Investment Strategy. Allowed values: {string.Join(", ", strategyRules.Select(r => r.RiskPreference))}.");

            if (!hasAgeBasedAllocationMaster())
                return (false, "Age-based allocation master is missing. Please load the age-band allocation table for Blue Chip Equity Fund and Gilt Fund.");

            // Age-based uses backend master allocation; clear any UI-provided allocations.
            req.FundAllocations.Clear();
        }
        else
        {
            // Self-managed: risk preference not applicable
            req.RiskPreference = null;
            if (req.FundAllocations.Count == 0)
                return (false, "At least one fund allocation is required for Self-Managed Investment Strategy.");

            var total = req.FundAllocations.Sum(f => f.AllocationPercent);
            if (Math.Abs(total - 100m) > 0.01m)
                return (false, $"Fund allocations must sum to 100%. Current sum: {total}%.");

            var belowMin = req.FundAllocations.FirstOrDefault(f => f.AllocationPercent > 0 && f.AllocationPercent < 10m);
            if (belowMin != null)
                return (false, $"Each selected fund must have at least 10% allocation. '{belowMin.FundType}' has {belowMin.AllocationPercent}%.");

            var invalidAlloc = req.FundAllocations.FirstOrDefault(f => f.AllocationPercent % 5m != 0);
            if (invalidAlloc != null)
                return (false, "Each fund allocation must be in multiples of 5% for Self-Managed Investment Strategy.");
        }

        return (true, null);
    }

    public static bool HasAgeBasedAllocationMaster(InsuranceDbContext db, string productCode) =>
        db.ProductParameters.Any(p =>
            p.Name.Contains("AgeBasedAllocation", StringComparison.OrdinalIgnoreCase) &&
            p.ProductVersion != null &&
            p.ProductVersion.Product != null &&
            p.ProductVersion.Product.Code == productCode);

    private static void NormalizeAliases(UlipCalculationRequest req)
    {
        var strategy = req.InvestmentStrategy?.Trim();
        if (strategy == null) return;

        if (strategy.Equals("Self-Managed", StringComparison.OrdinalIgnoreCase))
            req.InvestmentStrategy = "Self-Managed Investment Strategy";
        else if (strategy.Equals("Life-Stage Aggressive", StringComparison.OrdinalIgnoreCase))
        {
            req.InvestmentStrategy = "Age-based Investment Strategy";
            req.RiskPreference ??= "Aggressive";
        }
        else if (strategy.Equals("Life-Stage Conservative", StringComparison.OrdinalIgnoreCase))
        {
            req.InvestmentStrategy = "Age-based Investment Strategy";
            req.RiskPreference ??= "Conservative";
        }
    }

    private static IReadOnlyList<RiskPreferenceRule> LoadRules()
    {
        var path = FindRulesPath();
        if (path == null) return Array.Empty<RiskPreferenceRule>();

        var lines = File.ReadAllLines(path)
            .Skip(1) // header
            .Where(l => !string.IsNullOrWhiteSpace(l));

        var rules = new List<RiskPreferenceRule>();
        foreach (var line in lines)
        {
            var cols = line.Split(',', StringSplitOptions.TrimEntries);
            if (cols.Length < 3) continue;
            var isRequired = cols[2].Equals("yes", StringComparison.OrdinalIgnoreCase);
            rules.Add(new RiskPreferenceRule(cols[0], cols[1], isRequired));
        }

        return rules;
    }

    private static string? FindRulesPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var current = new DirectoryInfo(baseDir);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "docs", "ewealth_risk_preference_rules.csv");
            if (File.Exists(candidate))
                return candidate;
            var direct = Path.Combine(current.FullName, "ewealth_risk_preference_rules.csv");
            if (File.Exists(direct))
                return direct;
            current = current.Parent;
        }

        return null;
    }
}
