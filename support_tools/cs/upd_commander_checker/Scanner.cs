using System.Text.RegularExpressions;

namespace UpdCommanderChecker;

internal static class Scanner
{
    private static readonly Regex UsingPattern = new(
        @"^\s*using\s+([A-Za-z0-9_.]+)\s*;",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex LoopPattern = new(
        @"\b(for|foreach|while)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex CalculationPattern = new(
        @"[^+*/%<>=!-][+*/%][^=+*/]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DirectWorkPattern = new(
        @"\b(File|Directory|JsonSerializer|HttpClient|SqlConnection|DbConnection)\s*[.(]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static List<Finding> ScanPath(string target, IReadOnlyList<string> cliIgnore)
    {
        var root = Directory.Exists(target)
            ? Path.GetFullPath(target)
            : Path.GetDirectoryName(Path.GetFullPath(target)) ?? Directory.GetCurrentDirectory();
        var ignoreRules = IgnoreRules.Load(root);
        var files = File.Exists(target)
            ? [Path.GetFullPath(target)]
            : Directory.EnumerateFiles(target, "*.cs", SearchOption.AllDirectories).ToList();

        var findings = new List<Finding>();
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (cliIgnore.Any(pattern => IgnoreRules.GlobMatch(relative, pattern)))
            {
                continue;
            }
            findings.AddRange(ScanFile(file, relative, ignoreRules));
        }

        return findings
            .OrderBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Line)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<Finding> ScanFile(
        string file,
        string relative,
        IReadOnlyList<IgnoreRule> ignoreRules)
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(file);
        }
        catch
        {
            return [new Finding(relative, 1, "UPD001", "read failed")];
        }

        var source = Classifier.ClassifyPath(relative);
        var findings = new List<Finding>();
        for (var index = 0; index < lines.Length; index++)
        {
            var lineNumber = index + 1;
            var lineText = lines[index];
            var match = UsingPattern.Match(lineText);
            if (match.Success)
            {
                var target = Classifier.ClassifyReference(match.Groups[1].Value);
                var message = DependencyRules.GetError(source, target);
                if (message is not null)
                {
                    var crossApplication = source.ApplicationId.Length > 0 &&
                        target.ApplicationId.Length > 0 &&
                        source.ApplicationId != target.ApplicationId;
                    AddFinding(
                        findings,
                        relative,
                        lineNumber,
                        crossApplication ? "UPD102" : "UPD101",
                        message,
                        "error",
                        lineText,
                        ignoreRules);
                }
            }

            if (source.Role != "commander")
            {
                continue;
            }
            if (LoopPattern.IsMatch(lineText))
            {
                AddFinding(findings, relative, lineNumber, "UPD201", "Commander loop", "warning", lineText, ignoreRules);
            }
            if (CalculationPattern.IsMatch(lineText))
            {
                AddFinding(findings, relative, lineNumber, "UPD202", "Commander calculation", "warning", lineText, ignoreRules);
            }
            if (DirectWorkPattern.IsMatch(lineText))
            {
                AddFinding(findings, relative, lineNumber, "UPD203", "Commander direct I/O/API call", "error", lineText, ignoreRules);
            }
        }
        return findings;
    }

    private static void AddFinding(
        List<Finding> findings,
        string path,
        int line,
        string code,
        string message,
        string severity,
        string lineText,
        IReadOnlyList<IgnoreRule> ignoreRules)
    {
        if (!IgnoreRules.IsIgnored(path, code, lineText, ignoreRules))
        {
            findings.Add(new Finding(path, line, code, message, severity));
        }
    }
}
