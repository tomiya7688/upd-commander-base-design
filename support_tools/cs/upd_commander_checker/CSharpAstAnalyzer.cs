using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal sealed record AstAnalysisInput(
    CompilationUnitSyntax Root,
    ModuleInfo Source,
    string Relative,
    IReadOnlyList<string> Lines,
    IReadOnlyList<IgnoreRule> IgnoreRules);

internal sealed record AstRuleContext(
    List<Finding> Findings,
    AstAnalysisInput Analysis);

internal sealed record NodeFindingInput(
    AstRuleContext Context,
    SyntaxNode Node,
    string Code,
    string Message,
    string Severity);

internal static class CSharpAstAnalyzer
{
    private const int MinReducibleLines = 10;
    private const double MinReductionRatio = 0.20;

    private static readonly HashSet<string> DirectWorkTypes =
        ["File", "Directory", "JsonSerializer", "HttpClient", "SqlConnection", "DbConnection"];

    internal static List<Finding> Analyze(AstAnalysisInput input)
    {
        var context = new AstRuleContext([], input);
        AnalyzeDependencies(context);
        AnalyzeCommander(context);
        AnalyzeContainers(context);
        context.Findings.AddRange(ResponsibilityRules.Check(new ResponsibilityCheckInput(
            input.Root,
            input.Lines,
            input.Relative,
            input.IgnoreRules)));
        return context.Findings;
    }

    private static void AnalyzeDependencies(AstRuleContext context)
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
                Add(new NodeFindingInput(context, usingDirective, code, message, "error"));
                continue;
            }

            var warning = DependencyRules.GetWarning(dependency);
            if (warning is not null)
            {
                Add(new NodeFindingInput(context, usingDirective, "UPD103", warning, "warning"));
            }
        }
    }

    private static void AnalyzeCommander(AstRuleContext context)
    {
        if (context.Analysis.Source.Role != "commander")
        {
            return;
        }

        foreach (var node in context.Analysis.Root.DescendantNodes())
        {
            if (node is ForStatementSyntax or ForEachStatementSyntax or
                WhileStatementSyntax or DoStatementSyntax)
            {
                Add(new NodeFindingInput(
                    context,
                    node,
                    "UPD201",
                    "Commander loop",
                    "warning"));
                continue;
            }

            if (node is BinaryExpressionSyntax binary && IsArithmetic(binary))
            {
                Add(new NodeFindingInput(
                    context,
                    binary,
                    "UPD202",
                    "Commander calculation",
                    "warning"));
                continue;
            }

            if (node is InvocationExpressionSyntax or ObjectCreationExpressionSyntax &&
                IsDirectWork(node))
            {
                Add(new NodeFindingInput(
                    context,
                    node,
                    "UPD203",
                    "Commander direct I/O/API call",
                    "error"));
            }
        }
    }

    private static void AnalyzeContainers(AstRuleContext context)
    {
        if (context.Analysis.Source.Role == "compresser")
        {
            return;
        }

        var reducibleLines = 0;
        SyntaxNode? firstOffendingNode = null;

        foreach (var method in context.Analysis.Root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
        {
            var parameterCount = method.ParameterList.Parameters.Count;
            var outputCount = method is MethodDeclarationSyntax declaration &&
                declaration.ReturnType is TupleTypeSyntax tuple
                    ? tuple.Elements.Count
                    : 0;
            var inputViolation = parameterCount > 1;
            var outputViolation = outputCount > 1;

            if (inputViolation)
            {
                firstOffendingNode ??= method;
                Add(new NodeFindingInput(
                    context,
                    method,
                    "UPD301",
                    "multiple inputs reduce readability; consider one Input Container",
                    "attention"));
            }
            if (outputViolation)
            {
                firstOffendingNode ??= method;
                Add(new NodeFindingInput(
                    context,
                    method,
                    "UPD302",
                    "multiple return values reduce readability; consider one Output Container",
                    "attention"));
            }

            if (inputViolation || outputViolation)
            {
                var signatureLines = GetMethodReducibleLines(method);
                var excessValues = Math.Max(0, parameterCount - 1) + Math.Max(0, outputCount - 1);
                reducibleLines += Math.Max(signatureLines, excessValues);
            }
        }

        var effectiveLines = CountEffectiveLines(context.Analysis.Lines);
        var substantialCompression = reducibleLines >= MinReducibleLines &&
            (double)reducibleLines / Math.Max(1, effectiveLines) >= MinReductionRatio;
        if (context.Analysis.Source.Role is "commander" or "messenger" &&
            substantialCompression && firstOffendingNode is not null)
        {
            Add(new NodeFindingInput(
                context,
                firstOffendingNode,
                "UPD303",
                "Compresser/Container introduction is expected to substantially reduce this Commander/Messenger",
                "warning"));
        }
    }

    private static bool IsArithmetic(BinaryExpressionSyntax expression)
    {
        return expression.IsKind(SyntaxKind.AddExpression) ||
               expression.IsKind(SyntaxKind.SubtractExpression) ||
               expression.IsKind(SyntaxKind.MultiplyExpression) ||
               expression.IsKind(SyntaxKind.DivideExpression) ||
               expression.IsKind(SyntaxKind.ModuloExpression);
    }

    private static bool IsDirectWork(SyntaxNode node)
    {
        var owner = node switch
        {
            InvocationExpressionSyntax invocation when invocation.Expression is MemberAccessExpressionSyntax member
                => member.Expression.ToString(),
            ObjectCreationExpressionSyntax creation => creation.Type.ToString(),
            _ => string.Empty,
        };
        if (owner.Length == 0)
        {
            return false;
        }

        return DirectWorkTypes.Any(type =>
            owner == type || owner.EndsWith("." + type, StringComparison.Ordinal));
    }

    private static int GetMethodReducibleLines(BaseMethodDeclarationSyntax method)
    {
        var span = method.ParameterList.GetLocation().GetLineSpan();
        return Math.Max(0, span.EndLinePosition.Line - span.StartLinePosition.Line);
    }

    private static int CountEffectiveLines(IReadOnlyList<string> lines)
    {
        return lines.Count(line => !string.IsNullOrWhiteSpace(line));
    }

    private static void Add(NodeFindingInput input)
    {
        var line = input.Node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var lineText = line > 0 && line <= input.Context.Analysis.Lines.Count
            ? input.Context.Analysis.Lines[line - 1]
            : string.Empty;
        if (IgnoreRules.IsIgnored(new IgnoreCheckInput(
                input.Context.Analysis.Relative,
                input.Code,
                lineText,
                input.Context.Analysis.IgnoreRules)))
        {
            return;
        }

        input.Context.Findings.Add(new Finding(
            input.Context.Analysis.Relative,
            line,
            input.Code,
            input.Message,
            input.Severity));
    }
}
