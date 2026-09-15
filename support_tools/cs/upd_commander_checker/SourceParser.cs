using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UpdCommanderChecker;

internal static class SourceParser
{
    internal static SourceParseResult Parse(ScanFileInput input)
    {
        string sourceText;
        try
        {
            sourceText = File.ReadAllText(input.File);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new SourceParseResult(
                null,
                new Finding(input.Relative, 1, "UPD001", "read failed")
            );
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: input.File);
        var syntaxError = syntaxTree
            .GetDiagnostics()
            .FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        if (syntaxError is not null)
        {
            var line = syntaxError.Location.GetLineSpan().StartLinePosition.Line + 1;
            return new SourceParseResult(
                null,
                new Finding(input.Relative, line, "UPD002", "syntax error")
            );
        }

        var lines = sourceText.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        return new SourceParseResult(
            new ParsedSource(
                input.File,
                input.Relative,
                syntaxTree,
                syntaxTree.GetCompilationUnitRoot(),
                lines
            ),
            null
        );
    }
}
