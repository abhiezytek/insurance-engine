using System.Globalization;
using System.IO;
using System.Linq;
using InsuranceEngine.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Data;

/// <summary>Utility to load Century Income factor tables from the CSV sources in /docs.</summary>
public static class CenturyIncomeFactorLoader
{
    internal const string GmbFile = "century_income_gmb_factors.csv";
    private const string GsvFile = "century_income_gsv_factors.csv";
    internal const string SsvFile = "century_income_ssv_factors.csv";

    /// <summary>Seed GMB/GSV/SSV factors from the product CSVs if the tables are empty.</summary>
    public static async Task SeedFromCsvAsync(InsuranceDbContext context, string? docsPath = null)
    {
        docsPath ??= ResolveDocsPath();

        if (!await context.GmbFactors.AnyAsync())
        {
            context.GmbFactors.AddRange(ReadGmbFactors(docsPath));
        }

        if (!await context.GsvFactors.AnyAsync())
        {
            context.GsvFactors.AddRange(ReadGsvFactors(docsPath));
        }

        if (!await context.SsvFactors.AnyAsync())
        {
            context.SsvFactors.AddRange(ReadSsvFactors(docsPath));
        }

        await context.SaveChangesAsync();
    }

    /// <summary>Locate the docs directory from common execution roots (app bin, repo root, test runner).</summary>
    public static string ResolveDocsPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "docs"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "docs")),
            Path.Combine(Directory.GetCurrentDirectory(), "docs"),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "docs")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "docs")),
        };

        foreach (var path in candidates)
        {
            if (Directory.Exists(path))
            {
                var missing = RequiredFiles()
                    .Where(f => !File.Exists(Path.Combine(path, f)))
                    .ToList();
                if (!missing.Any()) return path;
            }
        }

        throw new InvalidOperationException(
            $"Unable to locate docs directory for Century Income CSV factor loading. Ensure the /docs folder exists at the repository root and contains {string.Join(", ", RequiredFiles())}. Tried paths: {string.Join(", ", candidates)}");
    }

    private static IEnumerable<GmbFactor> ReadGmbFactors(string docsPath)
    {
        var path = Path.Combine(docsPath, GmbFile);
        return File.ReadAllLines(path)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(line =>
            {
                var parts = line.Split(',', StringSplitOptions.TrimEntries);
                var age = int.Parse(parts[0], CultureInfo.InvariantCulture);
                // CSV provides age-specific factors, so min/max collapse to a single age band.
                return new GmbFactor
                {
                    EntryAgeMin = age,
                    EntryAgeMax = age,
                    Option = NormalizeOption(parts[1]),
                    Ppt = int.Parse(parts[2], CultureInfo.InvariantCulture),
                    Pt = int.Parse(parts[3], CultureInfo.InvariantCulture),
                    Factor = ParseDecimal(parts[4])
                };
            })
            .ToList();
    }

    private static IEnumerable<GsvFactor> ReadGsvFactors(string docsPath)
    {
        var path = Path.Combine(docsPath, GsvFile);
        return File.ReadAllLines(path)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(line =>
            {
                var parts = line.Split(',', StringSplitOptions.TrimEntries);
                return new GsvFactor
                {
                    PolicyYear = int.Parse(parts[0], CultureInfo.InvariantCulture),
                    Ppt = int.Parse(parts[1], CultureInfo.InvariantCulture),
                    Pt = int.Parse(parts[2], CultureInfo.InvariantCulture),
                    FactorPercent = ParseDecimal(parts[3])
                };
            })
            .ToList();
    }

    private static IEnumerable<SsvFactor> ReadSsvFactors(string docsPath)
    {
        var path = Path.Combine(docsPath, SsvFile);
        var lines = File.ReadAllLines(path).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l));

        var factor1 = new Dictionary<(int ppt, int pt, int policyYear), decimal>();
        var factor2Rows = new List<(string option, int ppt, int pt, int policyYear, decimal factor2)>();

        foreach (var line in lines)
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            var factorType = parts[0];
            var optionRaw = parts[1];
            var policyYear = int.Parse(parts[2], CultureInfo.InvariantCulture);
            var ppt = int.Parse(parts[3], CultureInfo.InvariantCulture);
            var pt = int.Parse(parts[4], CultureInfo.InvariantCulture);
            var value = ParseDecimal(parts[5]);

            if (string.Equals(factorType, "SSV_FACTOR_1", StringComparison.OrdinalIgnoreCase))
            {
                factor1[(ppt, pt, policyYear)] = value;
            }
            else
            {
                var option = NormalizeOption(optionRaw);
                factor2Rows.Add((option, ppt, pt, policyYear, value));
            }
        }

        var results = new List<SsvFactor>();
        foreach (var row in factor2Rows)
        {
            if (!factor1.TryGetValue((row.ppt, row.pt, row.policyYear), out var f1))
            {
                throw new InvalidOperationException(
                    $"Missing SSV_FACTOR_1 in {SsvFile} for PPT={row.ppt}, PT={row.pt}, PY={row.policyYear}, option={row.option}.");
            }
            results.Add(new SsvFactor
            {
                Ppt = row.ppt,
                Pt = row.pt,
                Option = row.option,
                PolicyYear = row.policyYear,
                Factor1 = f1,
                Factor2 = row.factor2
            });
        }

        return results;
    }

    /// <summary>Canonicalises Century Income options to Immediate/Deferred/Twin.</summary>
    public static string NormalizeOption(string? option) =>
        option?.Trim().ToLowerInvariant() switch
        {
            "immediate income" or "immediate" => "Immediate",
            "deferred income" or "deferred" => "Deferred",
            "twin income" or "twin" => "Twin",
            null or "" => throw new InvalidOperationException("Option is required for Century Income factor lookup."),
            _ => throw new InvalidOperationException($"Unsupported option value '{option}'. Expected Immediate, Deferred, or Twin.")
        };

    private static decimal ParseDecimal(string value) =>
        decimal.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);

    private static IEnumerable<string> RequiredFiles() => new[] { GmbFile, GsvFile, SsvFile };
}
