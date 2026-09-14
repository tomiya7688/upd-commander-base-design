using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal static class ResponsibilityRules
{
    private const int MaxResponsibilityLines = 250;
    private const int MaxResponsibilityMethods = 12;

    internal static List<Finding> Check(ResponsibilityCheckInput input)
    {
        var findings = new List<Finding>();
        var types = input
            .Root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Where(type => type is not InterfaceDeclarationSyntax)
            .ToList();
        var majorTypes = types.Where(HasBehavior).ToList();

        foreach (var type in types)
        {
            var span = type.GetLocation().GetLineSpan();
            var line = span.StartLinePosition.Line + 1;
            var lineText = line > 0 && line <= input.Lines.Count ? input.Lines[line - 1] : string.Empty;
            var lineCount = span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
            var methodCount = type.Members.OfType<MethodDeclarationSyntax>().Count();
            if (lineCount <= MaxResponsibilityLines && methodCount <= MaxResponsibilityMethods)
            {
                continue;
            }
            if (
                IgnoreRules.IsIgnored(
                    new IgnoreCheckInput(input.Path, "UPD401", lineText, input.IgnoreRules)
                )
            )
            {
                continue;
            }

            findings.Add(
                new Finding(
                    input.Path,
                    line,
                    "UPD401",
                    $"type {type.Identifier.ValueText} is too large for one responsibility "
                        + $"(lines={lineCount}, methods={methodCount})",
                    "warning"
                )
            );
        }

        var firstLineText = input.Lines.Count > 0 ? input.Lines[0] : string.Empty;
        if (
            types.Count == 0
            && input.Lines.Count > MaxResponsibilityLines
            && !IgnoreRules.IsIgnored(
                new IgnoreCheckInput(input.Path, "UPD401", firstLineText, input.IgnoreRules)
            )
        )
        {
            findings.Add(
                new Finding(
                    input.Path,
                    1,
                    "UPD401",
                    "file/module approximation is too large for one responsibility",
                    "warning"
                )
            );
        }

        if (majorTypes.Count > 1)
        {
            var second = majorTypes[1];
            var line = second.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var lineText = line > 0 && line <= input.Lines.Count ? input.Lines[line - 1] : string.Empty;
            if (
                !IgnoreRules.IsIgnored(
                    new IgnoreCheckInput(input.Path, "UPD402", lineText, input.IgnoreRules)
                )
            )
            {
                findings.Add(
                    new Finding(
                        input.Path,
                        line,
                        "UPD402",
                        "file contains multiple responsibility-bearing types",
                        "warning"
                    )
                );
            }
        }

        return findings;
    }

    private static bool HasBehavior(TypeDeclarationSyntax type)
    {
        return type.Members.Any(MemberHasBehavior);
    }

    private static bool MemberHasBehavior(MemberDeclarationSyntax member)
    {
        if (
            member
            is MethodDeclarationSyntax
                or ConstructorDeclarationSyntax
                or DestructorDeclarationSyntax
                or OperatorDeclarationSyntax
                or ConversionOperatorDeclarationSyntax
                or IndexerDeclarationSyntax
                or EventDeclarationSyntax
        )
        {
            return true;
        }
        if (member is PropertyDeclarationSyntax property)
        {
            return property.ExpressionBody is not null
                || property.AccessorList?.Accessors.Any(accessor =>
                    accessor.Body is not null || accessor.ExpressionBody is not null
                ) == true;
        }
        return false;
    }
}
