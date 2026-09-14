using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal static class DependencyAnalyzer
{
    internal static void Analyze(AstRuleContext context)
    {
        foreach (
            var usingDirective in context
                .Analysis.Root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
        )
        {
            var reference = usingDirective.Name?.ToString();
            if (string.IsNullOrWhiteSpace(reference))
            {
                continue;
            }

            var target = Classifier.ClassifyReference(reference);
            var result = DependencyRules.Evaluate(
                new DependencyCheckInput(context.Analysis.Source, target)
            );
            if (result is null)
            {
                continue;
            }

            AstFindingEmitter.Add(
                new NodeFindingInput(
                    context,
                    usingDirective,
                    result.Code,
                    result.Message,
                    result.Severity
                )
            );
        }
    }
}
