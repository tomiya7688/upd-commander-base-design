using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal static class DependencyAnalyzer
{
    internal static void Analyze(AstRuleContext context)
    {
        foreach (var usingDirective in context.Analysis.Root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            var reference = usingDirective.Name?.ToString();
            if (string.IsNullOrWhiteSpace(reference))
            {
                continue;
            }

            var target = Classifier.ClassifyReference(reference);
            var dependency = new DependencyCheckInput(context.Analysis.Source, target);
            var message = DependencyRules.GetError(dependency);
            if (message is not null)
            {
                var code = message == "cross-application internal dependency"
                    ? "UPD102"
                    : "UPD101";
                AstFindingEmitter.Add(new NodeFindingInput(
                    context,
                    usingDirective,
                    code,
                    message,
                    "error"));
                continue;
            }

            var warning = DependencyRules.GetWarning(dependency);
            if (warning is not null)
            {
                AstFindingEmitter.Add(new NodeFindingInput(
                    context,
                    usingDirective,
                    "UPD103",
                    warning,
                    "warning"));
            }
        }
    }
}
