using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal sealed record ResponsibilityCheckInput(
    CompilationUnitSyntax Root,
    IReadOnlyList<string> Lines,
    string Path,
    IReadOnlyList<IgnoreRule> IgnoreRules);
