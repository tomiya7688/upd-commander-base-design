using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal static class DataTypeLocationRules
{
    private sealed record DataTypeSource(string File, string Relative, SyntaxTree Tree, CompilationUnitSyntax Root);
    private sealed record DataTypeCandidate(string File, string Relative, int Line, string Name, INamedTypeSymbol Symbol);
    private sealed record ParseInput(IReadOnlyList<string> Files, string Root);
    private sealed record ReferenceInput(CompilationUnitSyntax Root, SemanticModel Model, INamedTypeSymbol Symbol);

    internal static List<Finding> Check(DataTypeLocationRuleContext input)
    {
        var sources = Parse(new ParseInput(input.Files, input.Root));
        var compilation = CSharpCompilation.Create(
            "UpdCommanderDataTypeReferenceAnalysis",
            sources.Select(source => source.Tree),
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var candidates = new List<DataTypeCandidate>();
        foreach (var source in sources)
        {
            var types = FileScopeTypes(source.Root).ToList();
            if (types.Count < 2)
            {
                continue;
            }
            var model = compilation.GetSemanticModel(source.Tree, ignoreAccessibility: true);
            foreach (var type in types.Where(IsDataOnly))
            {
                if (model.GetDeclaredSymbol(type) is not INamedTypeSymbol symbol)
                {
                    continue;
                }
                candidates.Add(new DataTypeCandidate(
                    source.File,
                    source.Relative,
                    type.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    type.Identifier.ValueText,
                    symbol));
            }
        }

        var findings = new List<Finding>();
        foreach (var candidate in candidates)
        {
            var external = sources.Any(source =>
                !string.Equals(source.File, candidate.File, StringComparison.OrdinalIgnoreCase) &&
                References(new ReferenceInput(
                    source.Root,
                    compilation.GetSemanticModel(source.Tree, ignoreAccessibility: true),
                    candidate.Symbol)));
            var code = external ? "UPD404" : "UPD403";
            if (IgnoreRules.IsIgnored(new IgnoreCheckInput(
                    candidate.Relative,
                    code,
                    string.Empty,
                    input.IgnoreRules)))
            {
                continue;
            }
            findings.Add(new Finding(
                candidate.Relative,
                candidate.Line,
                code,
                external
                    ? $"data-only type {candidate.Name} shares a file with another type and is referenced from another file"
                    : $"data-only type {candidate.Name} shares a file with another type",
                external ? "warning" : "attention"));
        }
        return findings;
    }

    private static IEnumerable<TypeDeclarationSyntax> FileScopeTypes(CompilationUnitSyntax root)
    {
        return root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Where(type => type.Parent is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax);
    }

    private static List<DataTypeSource> Parse(ParseInput input)
    {
        var result = new List<DataTypeSource>();
        foreach (var file in input.Files)
        {
            string text;
            try
            {
                text = File.ReadAllText(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }
            var tree = CSharpSyntaxTree.ParseText(text, path: file);
            if (tree.GetDiagnostics().Any(diagnostic =>
                    diagnostic.Severity == DiagnosticSeverity.Error))
            {
                continue;
            }
            result.Add(new DataTypeSource(
                file,
                Path.GetRelativePath(input.Root, file).Replace('\\', '/'),
                tree,
                tree.GetCompilationUnitRoot()));
        }
        return result;
    }

    private static bool IsDataOnly(TypeDeclarationSyntax type)
    {
        return !type.Members.Any(HasBehavior);
    }

    private static bool HasBehavior(MemberDeclarationSyntax member)
    {
        if (member is MethodDeclarationSyntax or ConstructorDeclarationSyntax or
            DestructorDeclarationSyntax or OperatorDeclarationSyntax or
            ConversionOperatorDeclarationSyntax or IndexerDeclarationSyntax or
            EventDeclarationSyntax)
        {
            return true;
        }
        if (member is PropertyDeclarationSyntax property)
        {
            return property.ExpressionBody is not null ||
                property.AccessorList?.Accessors.Any(accessor =>
                    accessor.Body is not null || accessor.ExpressionBody is not null) == true;
        }
        return false;
    }

    private static bool References(ReferenceInput input)
    {
        foreach (var node in input.Root.DescendantNodes().Where(node =>
                     node is IdentifierNameSyntax or GenericNameSyntax))
        {
            var symbol = input.Model.GetSymbolInfo(node).Symbol;
            if (symbol is INamedTypeSymbol typeSymbol &&
                SymbolEqualityComparer.Default.Equals(
                    typeSymbol.OriginalDefinition,
                    input.Symbol.OriginalDefinition))
            {
                return true;
            }
        }
        return false;
    }
}
