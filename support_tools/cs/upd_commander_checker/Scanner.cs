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

    private static readonly Regex MethodPattern = new(
        @"^\s*(?:public|protected|internal)\s+(?:static\s+|virtual\s+|override\s+|async\s+|sealed\s+)*[^=;]+?\s+[A-Za-z_][A-Za-z0-9_]*\s*\(([^()]*)\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TupleReturnPattern = new(
        @"^\s*(?:public|protected|internal)\s+(?:static\s+|virtual\s+|override\s+|async\s+|sealed\s+)*\([^)]*,[^)]*\)\s+[A-Za-z_][A-Za-z0-9_]*\s*\(",
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
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
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
                    var code = message == "cross-application internal dependency"
                        ? "UPD102"
                        : "UPD101";
                    AddFinding(findings, relative, lineNumber, code, message, "error", lineText, ignoreRules);
                }
            }

            if (IsComponentRole(source.Role))
            {
                var methodMatch = MethodPattern.Match(lineText);
                if (methodMatch.Success && CountParameters(methodMatch.Groups[1].Value) > 1)
                {
                    AddFinding(
                        findings,
                        relative,
                        lineNumber,
                        "UPD301",
                        "class operation has multiple inputs; use one Input Container",
                        "warning",
                        lineText,
                        ignoreRules);
                }
                if (TupleReturnPattern.IsMatch(lineText))
                {
                    AddFinding(
                        findings,
                        relative,
                        lineNumber,
                        "UPD302",
                        "class operation returns multiple values; use one Output Container",
                        "warning",
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

    private static bool IsComponentRole(string role) =>
        role is "commander" or "messenger" or "processing";

    private static int CountParameters(string parameters)
    {
        var text = parameters.Trim();
        if (text.Length == 0)
        {
            return 0;
        }
        return text.Count(ch => ch == ',') + 1;
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
