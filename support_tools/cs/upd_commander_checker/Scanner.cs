using System.Text.RegularExpressions;

namespace UpdCommanderChecker;

internal static class Scanner
{
    private const int MinReducibleLines = 10;
    private const double MinReductionRatio = 0.20;

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
        @"^\s*(?:public|protected|internal|private)\s+(?:static\s+|virtual\s+|override\s+|async\s+|sealed\s+)*[^=;]+?\s+[A-Za-z_][A-Za-z0-9_]*\s*\(([^()]*)\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TupleReturnPattern = new(
        @"^\s*(?:public|protected|internal|private)\s+(?:static\s+|virtual\s+|override\s+|async\s+|sealed\s+)*\([^)]*,[^)]*\)\s+[A-Za-z_][A-Za-z0-9_]*\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static List<Finding> ScanPath(ScanPathInput input)
    {
        var root = Directory.Exists(input.Target)
            ? Path.GetFullPath(input.Target)
            : Path.GetDirectoryName(Path.GetFullPath(input.Target)) ?? Directory.GetCurrentDirectory();
        var ignoreRules = IgnoreRules.Load(root);
        var files = File.Exists(input.Target)
            ? [Path.GetFullPath(input.Target)]
            : Directory.EnumerateFiles(input.Target, "*.cs", SearchOption.AllDirectories).ToList();

        var findings = new List<Finding>();
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (input.CliIgnore.Any(pattern =>
                    IgnoreRules.GlobMatch(new GlobMatchInput(relative, pattern))))
            {
                continue;
            }
            findings.AddRange(ScanFile(new ScanFileInput(file, relative, ignoreRules)));
        }

        return findings
            .OrderBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Line)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<Finding> ScanFile(ScanFileInput input)
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(input.File);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [new Finding(input.Relative, 1, "UPD001", "read failed")];
        }

        var source = Classifier.ClassifyPath(input.Relative);
        var findings = new List<Finding>();
        var reducibleLines = 0;
        var firstOffendingLine = 1;

        for (var index = 0; index < lines.Length; index++)
        {
            var lineNumber = index + 1;
            var lineText = lines[index];
            var match = UsingPattern.Match(lineText);
            if (match.Success)
            {
                var target = Classifier.ClassifyReference(match.Groups[1].Value);
                var message = DependencyRules.GetError(new DependencyCheckInput(source, target));
                if (message is not null)
                {
                    var code = message == "cross-application internal dependency"
                        ? "UPD102"
                        : "UPD101";
                    AddFinding(new AddFindingInput(
                        findings,
                        input.Relative,
                        lineNumber,
                        code,
                        message,
                        "error",
                        lineText,
                        input.IgnoreRules));
                }
            }

            if (source.Role != "compresser")
            {
                var methodMatch = MethodPattern.Match(lineText);
                var parameterCount = methodMatch.Success
                    ? CountParameters(methodMatch.Groups[1].Value)
                    : 0;
                var inputViolation = parameterCount > 1;
                var outputViolation = TupleReturnPattern.IsMatch(lineText);

                if (inputViolation)
                {
                    if (reducibleLines == 0)
                    {
                        firstOffendingLine = lineNumber;
                    }
                    AddFinding(new AddFindingInput(
                        findings,
                        input.Relative,
                        lineNumber,
                        "UPD301",
                        "multiple inputs reduce readability; consider one Input Container",
                        "attention",
                        lineText,
                        input.IgnoreRules));
                }
                if (outputViolation)
                {
                    if (reducibleLines == 0)
                    {
                        firstOffendingLine = lineNumber;
                    }
                    AddFinding(new AddFindingInput(
                        findings,
                        input.Relative,
                        lineNumber,
                        "UPD302",
                        "multiple return values reduce readability; consider one Output Container",
                        "attention",
                        lineText,
                        input.IgnoreRules));
                }
                if (inputViolation || outputViolation)
                {
                    reducibleLines += Math.Max(0, parameterCount - 1) + (outputViolation ? 1 : 0);
                }
            }

            if (source.Role != "commander")
            {
                continue;
            }
            if (LoopPattern.IsMatch(lineText))
            {
                AddFinding(new AddFindingInput(
                    findings,
                    input.Relative,
                    lineNumber,
                    "UPD201",
                    "Commander loop",
                    "warning",
                    lineText,
                    input.IgnoreRules));
            }
            if (CalculationPattern.IsMatch(lineText))
            {
                AddFinding(new AddFindingInput(
                    findings,
                    input.Relative,
                    lineNumber,
                    "UPD202",
                    "Commander calculation",
                    "warning",
                    lineText,
                    input.IgnoreRules));
            }
            if (DirectWorkPattern.IsMatch(lineText))
            {
                AddFinding(new AddFindingInput(
                    findings,
                    input.Relative,
                    lineNumber,
                    "UPD203",
                    "Commander direct I/O/API call",
                    "error",
                    lineText,
                    input.IgnoreRules));
            }
        }

        var effectiveLines = Math.Max(1, lines.Count(line => !string.IsNullOrWhiteSpace(line)));
        var substantialCompression = reducibleLines >= MinReducibleLines &&
            (double)reducibleLines / effectiveLines >= MinReductionRatio;
        if (source.Role is "commander" or "messenger" && substantialCompression)
        {
            AddFinding(new AddFindingInput(
                findings,
                input.Relative,
                firstOffendingLine,
                "UPD303",
                "Compresser/Container introduction is expected to substantially reduce this Commander/Messenger",
                "warning",
                lines.ElementAtOrDefault(firstOffendingLine - 1) ?? string.Empty,
                input.IgnoreRules));
        }

        return findings;
    }

    private static int CountParameters(string parameters)
    {
        var text = parameters.Trim();
        if (text.Length == 0)
        {
            return 0;
        }
        return text.Count(ch => ch == ',') + 1;
    }

    private static void AddFinding(AddFindingInput input)
    {
        if (!IgnoreRules.IsIgnored(new IgnoreCheckInput(
                input.Path,
                input.Code,
                input.LineText,
                input.IgnoreRules)))
        {
            input.Findings.Add(new Finding(
                input.Path,
                input.Line,
                input.Code,
                input.Message,
                input.Severity));
        }
    }
}
