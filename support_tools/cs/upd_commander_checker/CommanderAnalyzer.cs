using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal static class CommanderAnalyzer
{
    private static readonly HashSet<string> DirectWorkTypes =
    [
        "System.IO.File",
        "System.IO.Directory",
        "System.Text.Json.JsonSerializer",
        "System.Net.Http.HttpClient",
        "System.Data.Common.DbConnection",
    ];

    private static readonly IReadOnlyList<MetadataReference> RuntimeReferences = LoadRuntimeReferences();

    internal static void Analyze(AstRuleContext context)
    {
        if (context.Analysis.Source.Role != "commander")
        {
            return;
        }

        var semanticModel = CreateSemanticModel(context.Analysis.Root.SyntaxTree);
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

            if (node is InvocationExpressionSyntax or ObjectCreationExpressionSyntax &&
                IsDirectWork(node, semanticModel))
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

    private static bool IsDirectWork(SyntaxNode node, SemanticModel semanticModel)
    {
        ITypeSymbol? type = node switch
        {
            InvocationExpressionSyntax invocation =>
                (semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol)?.ContainingType,
            ObjectCreationExpressionSyntax creation => semanticModel.GetTypeInfo(creation).Type,
            _ => null,
        };
        return IsDirectWorkType(type);
    }

    private static bool IsDirectWorkType(ITypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var qualifiedName = current.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            if (DirectWorkTypes.Contains(qualifiedName))
            {
                return true;
            }
        }
        return false;
    }

    private static SemanticModel CreateSemanticModel(SyntaxTree syntaxTree)
    {
        var compilation = CSharpCompilation.Create(
            "UpdCommanderCheckerSemanticAnalysis",
            [syntaxTree],
            RuntimeReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
    }

    private static IReadOnlyList<MetadataReference> LoadRuntimeReferences()
    {
        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrWhiteSpace(trustedAssemblies))
        {
            return [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];
        }

        return trustedAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }
}
