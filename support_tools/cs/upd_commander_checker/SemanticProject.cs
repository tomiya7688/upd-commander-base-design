using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UpdCommanderChecker;

internal sealed class SemanticProject
{
    private CSharpCompilation compilation = null!;
    private IReadOnlyDictionary<SyntaxTree, string> relativeByTree = null!;

    private SemanticProject() { }

    internal static SemanticProject Create(IReadOnlyList<ParsedSource> sources)
    {
        var trees = sources.Select(source => source.Tree).ToArray();
        var project = new SemanticProject
        {
            compilation = CSharpCompilation.Create(
                "UpdCommanderCheckerProjectAnalysis",
                trees,
                LoadRuntimeReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            ),
            relativeByTree = sources.ToDictionary(source => source.Tree, source => source.Relative),
        };
        return project;
    }

    internal SemanticModel GetModel(SyntaxTree tree)
    {
        return compilation.GetSemanticModel(tree, ignoreAccessibility: true);
    }

    internal IReadOnlyList<string> FindSourcePaths(ISymbol symbol)
    {
        var owner = DependencyOwner(symbol);
        if (owner is null)
        {
            return [];
        }

        return owner
            .Locations.Where(location => location.IsInSource && location.SourceTree is not null)
            .Select(location => location.SourceTree!)
            .Where(relativeByTree.ContainsKey)
            .Select(tree => relativeByTree[tree])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ISymbol? DependencyOwner(ISymbol symbol)
    {
        if (symbol is IAliasSymbol alias)
        {
            symbol = alias.Target;
        }
        if (symbol is INamedTypeSymbol namedType)
        {
            return namedType.OriginalDefinition;
        }
        return symbol.ContainingType?.OriginalDefinition;
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
