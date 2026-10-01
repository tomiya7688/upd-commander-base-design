using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal static class AstFindingEmitter
{
    internal static void Add(NodeFindingInput input)
    {
        var line = input.Node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var lineText =
            line > 0 && line <= input.Context.Analysis.Lines.Count
                ? input.Context.Analysis.Lines[line - 1]
                : string.Empty;
        if (
            IgnoreRules.IsIgnored(
                new IgnoreCheckInput(
                    input.Context.Analysis.Relative,
                    input.Code,
                    lineText,
                    input.Context.Analysis.IgnoreRules
                )
            )
        )
        {
            return;
        }

        input.Context.Findings.Add(
            new Finding(
                input.Context.Analysis.Relative,
                line,
                input.Code,
                input.Message,
                input.Severity,
                SemanticSymbol(input.Node),
                "syntax:"
                    + string.Join(
                        " ",
                        input
                            .Node.DescendantTokens()
                            .Select(token =>
                                token.Text.Replace("\0", "\\u0000", StringComparison.Ordinal)
                            )
                    )
            )
        );
    }

    private static string SemanticSymbol(SyntaxNode node)
    {
        var parts = new List<string>();
        var scope = node.AncestorsAndSelf()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();
        var type = node.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        var method = node.AncestorsAndSelf().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
        if (scope is not null)
            parts.Add(scope.Name.ToString());
        if (type is not null)
            parts.Add(type.Identifier.ValueText);
        if (method is MethodDeclarationSyntax declaration)
            parts.Add(declaration.Identifier.ValueText);
        else if (method is ConstructorDeclarationSyntax constructor)
            parts.Add(constructor.Identifier.ValueText);
        return string.Join(".", parts);
    }
}
