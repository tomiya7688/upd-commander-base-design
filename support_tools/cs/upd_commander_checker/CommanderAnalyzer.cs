using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal static class CommanderAnalyzer
{
    private static readonly HashSet<string> DirectWorkTypes =
        ["File", "Directory", "JsonSerializer", "HttpClient", "SqlConnection", "DbConnection"];

    internal static void Analyze(AstRuleContext context)
    {
        if (context.Analysis.Source.Role != "commander")
        {
            return;
        }

        foreach (var node in context.Analysis.Root.DescendantNodes())
        {
            if (node is ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax)
            {
                AstFindingEmitter.Add(new NodeFindingInput(context, node, "UPD201", "Commander loop", "warning"));
                continue;
            }

            if (node is BinaryExpressionSyntax binary && IsArithmetic(binary))
            {
                AstFindingEmitter.Add(new NodeFindingInput(context, binary, "UPD202", "Commander calculation", "warning"));
                continue;
            }

            if (node is InvocationExpressionSyntax or ObjectCreationExpressionSyntax && IsDirectWork(node))
            {
                AstFindingEmitter.Add(new NodeFindingInput(
                    context,
                    node,
                    "UPD203",
                    "Commander direct I/O/API call",
                    "error"));
            }
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
}
