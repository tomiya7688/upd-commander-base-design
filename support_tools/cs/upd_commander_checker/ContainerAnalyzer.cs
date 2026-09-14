using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal static class ContainerAnalyzer
{
    private const int MinReducibleLines = 10;
    private const double MinReductionRatio = 0.20;

    internal static void Analyze(AstRuleContext context)
    {
        if (context.Analysis.Source.Role == "compresser")
        {
            return;
        }

        var reducibleLines = 0;
        SyntaxNode? firstOffendingNode = null;

        foreach (
            var method in context
                .Analysis.Root.DescendantNodes()
                .OfType<BaseMethodDeclarationSyntax>()
        )
        {
            var parameterCount = method.ParameterList.Parameters.Count;
            var outputCount =
                method is MethodDeclarationSyntax declaration
                && declaration.ReturnType is TupleTypeSyntax tuple
                    ? tuple.Elements.Count
                    : 0;
            var inputViolation = parameterCount > 1;
            var outputViolation = outputCount > 1;

            if (inputViolation)
            {
                firstOffendingNode ??= method;
                AstFindingEmitter.Add(
                    new NodeFindingInput(
                        context,
                        method,
                        "UPD301",
                        "multiple inputs reduce readability; consider one Input Container",
                        "attention"
                    )
                );
            }
            if (outputViolation)
            {
                firstOffendingNode ??= method;
                AstFindingEmitter.Add(
                    new NodeFindingInput(
                        context,
                        method,
                        "UPD302",
                        "multiple return values reduce readability; consider one Output Container",
                        "attention"
                    )
                );
            }

            if (inputViolation || outputViolation)
            {
                var signatureLines = GetMethodReducibleLines(method);
                var excessValues = Math.Max(0, parameterCount - 1) + Math.Max(0, outputCount - 1);
                reducibleLines += Math.Max(signatureLines, excessValues);
            }
        }

        var effectiveLines = context.Analysis.Lines.Count(line => !string.IsNullOrWhiteSpace(line));
        var substantialCompression =
            reducibleLines >= MinReducibleLines
            && (double)reducibleLines / Math.Max(1, effectiveLines) >= MinReductionRatio;
        if (
            context.Analysis.Source.Role is "commander" or "messenger"
            && substantialCompression
            && firstOffendingNode is not null
        )
        {
            AstFindingEmitter.Add(
                new NodeFindingInput(
                    context,
                    firstOffendingNode,
                    "UPD303",
                    "Compresser/Container introduction is expected to substantially reduce this Commander/Messenger",
                    "warning"
                )
            );
        }
    }

    private static int GetMethodReducibleLines(BaseMethodDeclarationSyntax method)
    {
        var span = method.ParameterList.GetLocation().GetLineSpan();
        return Math.Max(0, span.EndLinePosition.Line - span.StartLinePosition.Line);
    }
}
