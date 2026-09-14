using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal sealed record AstAnalysisInput(
    CompilationUnitSyntax Root,
    ModuleInfo Source,
    string Relative,
    IReadOnlyList<string> Lines,
    IReadOnlyList<IgnoreRule> IgnoreRules);
