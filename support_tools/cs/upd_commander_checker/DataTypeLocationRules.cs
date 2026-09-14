using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal sealed record DataTypeSource(string File, string Relative, CompilationUnitSyntax Root);

internal sealed record DataTypeCandidate(string File, string Relative, int Line, string Name);

internal static class DataTypeLocationRules
{
    internal static List<Finding> Check(
        IReadOnlyList<string> files,
        string root,
        IReadOnlyList<IgnoreRule> ignoreRules)
    {
        var sources = Parse(files, root);
        var candidates = new List<DataTypeCandidate>();
        foreach (var source in sources)
        {
            var dataTypes = source.Root.Members
                .OfType<TypeDeclarationSyntax>()
                .Where(IsDataOnly)
                .ToList();
            if (dataTypes.Count < 2)
            {
                continue;
            }
            candidates.AddRange(dataTypes.Select(type => new DataTypeCandidate(
                source.File,
                source.Relative,
                type.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                type.Identifier.ValueText)));
        }

        var findings = new List<Finding>();
        foreach (var candidate in candidates)
        {
            var external = sources.Any(source =>
                !string.Equals(source.File, candidate.File, StringComparison.OrdinalIgnoreCase) &&
                References(source.Root, candidate.Name));
            var code = external ? "UPD404" : "UPD403";
            if (IgnoreRules.IsIgnored(new IgnoreCheckInput(
                    candidate.Relative,
                    code,
                    string.Empty,
                    ignoreRules)))
            {
                continue;
            }
            findings.Add(new Finding(
                candidate.Relative,
                candidate.Line,
                code,
                external
                    ? $"data-only type {candidate.Name} shares a file and is referenced from another file"
                    : $"multiple data-only types share this file; {candidate.Name} is local-only",
                external ? "warning" : "attention"));
        }
        return findings;
    }

    private static List<DataTypeSource> Parse(IReadOnlyList<string> files, string root)
    {
        var result = new List<DataTypeSource>();
        foreach (var file in files)
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
                    diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error))
            {
                continue;
            }
            result.Add(new DataTypeSource(
                file,
                Path.GetRelativePath(root, file).Replace('\\', '/'),
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

    private static bool References(CompilationUnitSyntax root, string name)
    {
        return root.DescendantNodes().Any(node =>
            node is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == name ||
            node is GenericNameSyntax generic && generic.Identifier.ValueText == name);
    }
}
