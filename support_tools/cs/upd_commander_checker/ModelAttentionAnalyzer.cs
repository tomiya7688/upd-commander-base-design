using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal static class ModelAttentionAnalyzer
{
    internal static IReadOnlyList<ModelGroupOccurrence> Collect(
        ParsedSource source,
        ModuleInfo module,
        IReadOnlyList<IgnoreRule> ignoreRules,
        int minItems
    )
    {
        var occurrences = new List<ModelGroupOccurrence>();

        foreach (var method in source.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            Add(
                occurrences,
                source,
                module,
                ignoreRules,
                minItems,
                method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                "parameters",
                ParameterKeys(method.ParameterList)
            );
        }

        foreach (var local in source.Root.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
        {
            Add(
                occurrences,
                source,
                module,
                ignoreRules,
                minItems,
                local.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                "parameters",
                ParameterKeys(local.ParameterList)
            );
        }

        foreach (var tuple in source.Root.DescendantNodes().OfType<TupleExpressionSyntax>())
        {
            var keys = tuple.Arguments.Select(argument => ItemKey(argument.Expression)).ToArray();
            if (keys.All(key => key.Length > 0))
            {
                Add(
                    occurrences,
                    source,
                    module,
                    ignoreRules,
                    minItems,
                    tuple.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "tuple",
                    keys
                );
            }
        }

        var statements = source
            .Root.DescendantNodes()
            .OfType<StatementSyntax>()
            .Where(statement =>
                statement is ExpressionStatementSyntax
                    or ReturnStatementSyntax
                    or LocalDeclarationStatementSyntax
            );
        foreach (var statement in statements)
        {
            var groups = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var access in statement.DescendantNodesAndSelf().OfType<ElementAccessExpressionSyntax>())
            {
                if (access.ArgumentList.Arguments.Count != 1)
                {
                    continue;
                }
                var collection = ItemKey(access.Expression);
                var index = IndexKey(access.ArgumentList.Arguments[0].Expression);
                if (collection.Length == 0 || index.Length == 0)
                {
                    continue;
                }
                if (!groups.TryGetValue(index, out var items))
                {
                    items = [];
                    groups[index] = items;
                }
                if (!items.Contains(collection, StringComparer.Ordinal))
                {
                    items.Add(collection);
                }
            }
            foreach (var items in groups.Values)
            {
                Add(
                    occurrences,
                    source,
                    module,
                    ignoreRules,
                    minItems,
                    statement.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "parallel_collection",
                    items
                );
            }
        }

        return occurrences;
    }

    internal static IEnumerable<Finding> BuildFindings(
        IReadOnlyList<ModelGroupOccurrence> occurrences,
        int minOccurrences
    )
    {
        foreach (var group in occurrences.GroupBy(item => item.Signature, StringComparer.Ordinal))
        {
            var ordered = group
                .OrderBy(item => item.Path, StringComparer.Ordinal)
                .ThenBy(item => item.Line)
                .ToArray();
            if (ordered.Length < minOccurrences)
            {
                continue;
            }
            var first = ordered[0];
            yield return new Finding(
                first.Path,
                first.Line,
                "UPD406",
                $"repeated value group may benefit from a Model/DTO; items={string.Join(',', first.Items)} occurrences={ordered.Length} kind={first.Kind}",
                "attention"
            );
        }
    }

    private static IReadOnlyList<string> ParameterKeys(ParameterListSyntax list)
    {
        return list
            .Parameters.Where(parameter => !parameter.Modifiers.Any(SyntaxKind.ThisKeyword))
            .Select(parameter => Normalize(parameter.Identifier.ValueText))
            .ToArray();
    }

    private static string ItemKey(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax identifier => Normalize(identifier.Identifier.ValueText),
            MemberAccessExpressionSyntax member => Normalize(member.Name.Identifier.ValueText),
            ElementAccessExpressionSyntax access => ItemKey(access.Expression),
            _ => string.Empty,
        };
    }

    private static string IndexKey(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax identifier => "name:" + Normalize(identifier.Identifier.ValueText),
            LiteralExpressionSyntax literal => "literal:" + literal.Token.ValueText,
            MemberAccessExpressionSyntax member => "member:" + Normalize(member.Name.Identifier.ValueText),
            _ => expression.NormalizeWhitespace().ToFullString(),
        };
    }

    private static void Add(
        List<ModelGroupOccurrence> occurrences,
        ParsedSource source,
        ModuleInfo module,
        IReadOnlyList<IgnoreRule> ignoreRules,
        int minItems,
        int line,
        string kind,
        IReadOnlyList<string> items
    )
    {
        if (
            items.Count < minItems
            || items.Any(string.IsNullOrWhiteSpace)
            || items.Distinct(StringComparer.Ordinal).Count() != items.Count
        )
        {
            return;
        }
        var lineText = line >= 1 && line <= source.Lines.Count ? source.Lines[line - 1] : string.Empty;
        if (
            IgnoreRules.IsIgnored(
                new IgnoreCheckInput(source.Relative, "UPD406", lineText, ignoreRules)
            )
        )
        {
            return;
        }
        occurrences.Add(
            new ModelGroupOccurrence(
                source.Relative,
                line,
                module.Application ?? string.Empty,
                module.Layer ?? string.Empty,
                kind,
                items
            )
        );
    }

    private static string Normalize(string value)
    {
        return value.TrimStart('_').ToLowerInvariant();
    }
}
