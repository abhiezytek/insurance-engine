using System;
using System.IO;
using System.Linq;

namespace InsuranceEngine.Tests;

internal static class DocFileHelper
{
    public static void EnsureDocFilesAvailable()
    {
        var baseDir = AppContext.BaseDirectory;
        var docsDir = Path.Combine(baseDir, "docs");
        Directory.CreateDirectory(docsDir);

        CopyDocIfAvailable("ewealth_risk_preference_rules.csv", docsDir,
            "InvestmentStrategy,RiskPreference,IsRequired\nSelf-Managed Investment Strategy,,no\n");
        CopyDocIfAvailable("ewealth_ppt_pt_rules.csv", docsDir,
            "TypeOfPpt,Ppt,PlanOption,LaMinorFlag,PtMin,PtMax\nLimited,10,Platinum,NA,5,30\n");
    }

    private static void CopyDocIfAvailable(string fileName, string docsDir, string fallbackContent)
    {
        var destPath = Path.Combine(docsDir, fileName);
        if (File.Exists(destPath))
        {
            var existing = File.ReadAllText(destPath);
            if (existing.Contains("Regular", StringComparison.OrdinalIgnoreCase) ||
                existing.Contains("Single", StringComparison.OrdinalIgnoreCase))
                return;

            File.Delete(destPath);
        }

        var source = FindDocInRepo(fileName);
        if (source != null)
        {
            File.Copy(source, destPath, overwrite: true);
            return;
        }

        if (!File.Exists(destPath))
            File.WriteAllText(destPath, fallbackContent);
    }

    private static string? FindDocInRepo(string fileName)
    {
        var roots = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."))
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            var current = new DirectoryInfo(root);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "docs", fileName);
                if (File.Exists(candidate)) return candidate;
                current = current.Parent;
            }
        }
        return null;
    }
}
