using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UpdCommanderChecker;

internal static class Scanner
{
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
        string sourceText;
        try
        {
            sourceText = File.ReadAllText(input.File);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [new Finding(input.Relative, 1, "UPD001", "read failed")];
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: input.File);
        var syntaxError = syntaxTree.GetDiagnostics()
            .FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        if (syntaxError is not null)
        {
            var line = syntaxError.Location.GetLineSpan().StartLinePosition.Line + 1;
            return [new Finding(input.Relative, line, "UPD002", "syntax error")];
        }

        var lines = sourceText
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .ToArray();
        var analysis = new AstAnalysisInput(
            syntaxTree.GetCompilationUnitRoot(),
            Classifier.ClassifyPath(input.Relative),
            input.Relative,
            lines,
            input.IgnoreRules);
        return CSharpAstAnalyzer.Analyze(analysis);
    }
}
