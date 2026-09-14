using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UpdCommanderChecker;

internal static class ResponsibilityRules
{
    private const int MaxResponsibilityLines = 350;

    internal static List<Finding> Check(ResponsibilityCheckInput input)
    {
        var findings = new List<Finding>();
        var types = input.Root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Where(type => type is not InterfaceDeclarationSyntax)
            .ToList();
        var majorTypes = types.Where(HasBehavior).ToList();

        foreach (var type in types)
        {
            var span = type.GetLocation().GetLineSpan();
            var lineCount = span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
            if (lineCount <= MaxResponsibilityLines)
            {
                continue;
            }
            if (IgnoreRules.IsIgnored(new IgnoreCheckInput(
                    input.Path,
                    "UPD401",
                    string.Empty,
                    input.IgnoreRules)))
            {
                continue;
            }

            findings.Add(new Finding(
                input.Path,
                span.StartLinePosition.Line + 1,
                "UPD401",
                $"type {type.Identifier.ValueText} is too large for one responsibility (lines={lineCount})",
                "warning"));
        }

        if (types.Count == 0 &&
            input.Lines.Count(line => !string.IsNullOrWhiteSpace(line)) > MaxResponsibilityLines &&
            !IgnoreRules.IsIgnored(new IgnoreCheckInput(
                input.Path,
                "UPD401",
                string.Empty,
                input.IgnoreRules)))
        {
            findings.Add(new Finding(
                input.Path,
                1,
                "UPD401",
                "file/module is too large for one responsibility",
                "warning"));
        }

        if (majorTypes.Count > 1 &&
            !IgnoreRules.IsIgnored(new IgnoreCheckInput(
                input.Path,
                "UPD402",
                string.Empty,
                input.IgnoreRules)))
        {
            var second = majorTypes[1];
            findings.Add(new Finding(
                input.Path,
                second.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                "UPD402",
                "file contains multiple responsibility-bearing types",
                "warning"));
        }

        return findings;
    }

    private static bool HasBehavior(TypeDeclarationSyntax type)
    {
        return type.Members.Any(member =>
            member is MethodDeclarationSyntax or ConstructorDeclarationSyntax or
            PropertyDeclarationSyntax or EventDeclarationSyntax);
    }
}
