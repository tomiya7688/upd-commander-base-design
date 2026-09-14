using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal sealed record AstAnalysisInput(
    CompilationUnitSyntax Root,
    ModuleInfo Source,
    string Relative,
    IReadOnlyList<string> Lines,
    IReadOnlyList<IgnoreRule> IgnoreRules);

internal static class CSharpAstAnalyzer
{
    internal static List<Finding> Analyze(AstAnalysisInput input)
    {
        var context = new AstRuleContext([], input);
        DependencyAnalyzer.Analyze(context);
        CommanderAnalyzer.Analyze(context);
        ContainerAnalyzer.Analyze(context);
        context.Findings.AddRange(ResponsibilityRules.Check(new ResponsibilityCheckInput(
            input.Root,
            input.Lines,
            input.Relative,
            input.IgnoreRules)));
        return context.Findings;
    }
}
