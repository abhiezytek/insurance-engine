using InsuranceEngine.Api.DTOs;
using System.Globalization;
using System.Threading;
using System.Linq;
using System.IO;

namespace InsuranceEngine.Api.Services;

internal record PptPtRule(string PptType, int Ppt, string PlanOption, string LaMinorFlag, int PtMin, int PtMax);

/// <summary>
/// CSV-backed validation for Type of PPT / PPT / PT combinations as captured
/// in <c>docs/ewealth_ppt_pt_rules.csv</c>. Keeps the validation table-driven
/// and decoupled from UI logic.
/// </summary>
internal static class PptPtRuleBook
{
    private const string RulesFileName = "ewealth_ppt_pt_rules.csv";
    private static readonly Lazy<IReadOnlyList<PptPtRule>> CachedRules = new(LoadRules, LazyThreadSafetyMode.ExecutionAndPublication);

    public static IReadOnlyList<PptPtRule> Rules => CachedRules.Value;

    public static (bool IsValid, string? Error) Validate(UlipCalculationRequest req, int entryAge, DateTime effectiveDate)
    {
        var rules = Rules;
        if (rules.Count == 0)
            return (false, $"PPT/PT rules are not configured. Ensure {RulesFileName} exists under docs/ and restart the service.");

        var pptType = req.TypeOfPpt?.Trim();
        if (string.IsNullOrWhiteSpace(pptType))
            return (false, "TypeOfPpt is required (Single, Limited, Regular).");

        var laMinorFlag = GetMinorFlag(req.Option, entryAge, effectiveDate);

        var matchingTypeRules = rules.Where(r =>
            r.PptType.Equals(pptType, StringComparison.OrdinalIgnoreCase) &&
            r.PlanOption.Equals(req.Option, StringComparison.OrdinalIgnoreCase) &&
            r.LaMinorFlag.Equals(laMinorFlag, StringComparison.OrdinalIgnoreCase)).ToList();

        if (matchingTypeRules.Count == 0)
        {
            var allowedTypes = rules
                .Where(r => r.PlanOption.Equals(req.Option, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.PptType)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            return (false, $"TypeOfPpt '{pptType}' is not allowed for plan option '{req.Option}'. Allowed: {string.Join(", ", allowedTypes)}.");
        }

        var pptRule = matchingTypeRules.FirstOrDefault(r => r.Ppt == req.Ppt);
        if (pptRule == null)
        {
            var allowedPpts = matchingTypeRules.Select(r => r.Ppt).Distinct().OrderBy(x => x);
            return (false, $"PPT '{req.Ppt}' is not allowed for TypeOfPpt '{pptType}' and plan option '{req.Option}'. Allowed PPT values: {string.Join(", ", allowedPpts)}.");
        }

        if (req.PolicyTerm < pptRule.PtMin || req.PolicyTerm > pptRule.PtMax)
            return (false, $"PolicyTerm must be between {pptRule.PtMin} and {pptRule.PtMax} for PPT {req.Ppt} ({pptType}) and plan option '{req.Option}'.");

        return (true, null);
    }

    private static string GetMinorFlag(string? option, int entryAge, DateTime effectiveDate)
    {
        if (!string.Equals(option, "Platinum Plus", StringComparison.OrdinalIgnoreCase))
            return "NA";

        // For Platinum Plus, rules vary for minor vs major lives.
        // Treat <18 as MINOR, >=18 as MAJOR. (EntryAge already normalised before this call.)
        var isMinor = entryAge > 0 && entryAge < 18;

        return isMinor ? "MINOR" : "MAJOR";
    }

    private static IReadOnlyList<PptPtRule> LoadRules()
    {
        var path = FindRulesPath();
        if (path == null) return Array.Empty<PptPtRule>();

        var lines = File.ReadAllLines(path)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l));

        var list = new List<PptPtRule>();
        foreach (var line in lines)
        {
            var cols = line.Split(',', StringSplitOptions.TrimEntries);
            if (cols.Length < 6) continue;

            if (!int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ppt)) continue;
            if (!int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ptMin)) continue;
            if (!int.TryParse(cols[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ptMax)) continue;

            list.Add(new PptPtRule(cols[0], ppt, cols[2], cols[3], ptMin, ptMax));
        }

        return list;
    }

    private static string? FindRulesPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "docs", RulesFileName);
            if (File.Exists(candidate)) return candidate;
            var direct = Path.Combine(current.FullName, RulesFileName);
            if (File.Exists(direct)) return direct;
            current = current.Parent;
        }

        return null;
    }
}
