using System.Text.RegularExpressions;

namespace UpdCommanderChecker;

internal static class Scanner
{
    private const int BloatOperationThreshold = 3;
    private const int BloatExcessSlotThreshold = 6;

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
        var offendingOperations = 0;
        var excessSlots = 0;
        var firstOffendingLine = 1;

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
                var operationOffends = false;
                var methodMatch = MethodPattern.Match(lineText);
                if (methodMatch.Success)
                {
                    var parameterCount = CountParameters(methodMatch.Groups[1].Value);
                    if (parameterCount > 1)
                    {
                        operationOffends = true;
                        excessSlots += parameterCount - 1;
                        AddFinding(
                            findings,
                            relative,
                            lineNumber,
                            "UPD301",
                            "multiple inputs reduce readability; consider one Input Container",
                            "attention",
                            lineText,
                            ignoreRules);
                    }
                }
                if (TupleReturnPattern.IsMatch(lineText))
                {
                    operationOffends = true;
                    excessSlots += 1;
                    AddFinding(
                        findings,
                        relative,
                        lineNumber,
                        "UPD302",
                        "multiple return values reduce readability; consider one Output Container",
                        "attention",
                        lineText,
                        ignoreRules);
                }
                if (operationOffends)
                {
                    if (offendingOperations == 0)
                    {
                        firstOffendingLine = lineNumber;
                    }
                    offendingOperations++;
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

        if (source.Role is "commander" or "messenger" &&
            (offendingOperations >= BloatOperationThreshold || excessSlots >= BloatExcessSlotThreshold))
        {
            AddFinding(
                findings,
                relative,
                firstOffendingLine,
                "UPD303",
                "uncontainerized signatures contribute to Commander/Messenger bloat",
                "warning",
                lines.ElementAtOrDefault(firstOffendingLine - 1) ?? string.Empty,
                ignoreRules);
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
