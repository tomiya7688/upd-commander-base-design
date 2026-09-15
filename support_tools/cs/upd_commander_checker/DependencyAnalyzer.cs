using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal static class DependencyAnalyzer
{
    internal static void Analyze(AstRuleContext context)
    {
        var project = context.Analysis.SemanticProject;
        var model = project.GetModel(context.Analysis.Root.SyntaxTree);
        var emitted = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in context.Analysis.Root.DescendantNodes().Where(IsReferenceNode))
        {
            if (node.AncestorsAndSelf().Any(ancestor => ancestor is UsingDirectiveSyntax))
            {
                continue;
            }

            ISymbol? symbol = null;
            if (node is IdentifierNameSyntax identifier)
            {
                symbol = model.GetAliasInfo(identifier)?.Target;
            }
            symbol ??= model.GetSymbolInfo(node).Symbol;
            if (symbol is null)
            {
                continue;
            }

            foreach (var targetPath in project.FindSourcePaths(symbol))
            {
                if (
                    string.Equals(
                        targetPath,
                        context.Analysis.Relative,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    continue;
                }

                var target = Classifier.ClassifyPath(targetPath);
                var result = DependencyRules.Evaluate(
                    new DependencyCheckInput(context.Analysis.Source, target)
                );
                if (result is null)
                {
                    continue;
                }

                var line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                var key = $"{line}:{targetPath}:{result.Code}";
                if (!emitted.Add(key))
                {
                    continue;
                }

                AstFindingEmitter.Add(
                    new NodeFindingInput(
                        context,
                        node,
                        result.Code,
                        result.Message,
                        result.Severity
                    )
                );
            }
        }
    }

    private static bool IsReferenceNode(SyntaxNode node)
    {
        return node is IdentifierNameSyntax or GenericNameSyntax;
    }
}
