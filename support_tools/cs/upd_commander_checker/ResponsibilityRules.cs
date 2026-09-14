using System.Text.RegularExpressions;

namespace UpdCommanderChecker;

internal static class ResponsibilityRules
{
    private const int MaxResponsibilityLines = 250;

    private static readonly Regex ClassPattern = new(
        @"^\s*(?:(?:public|internal|private|protected|static|sealed|abstract|partial)\s+)*class\s+[A-Za-z_][A-Za-z0-9_]*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static List<Finding> Check(ResponsibilityCheckInput input)
    {
        var findings = new List<Finding>();
        var nonBlankLines = input.Lines.Count(line => !string.IsNullOrWhiteSpace(line));
        if (nonBlankLines > MaxResponsibilityLines &&
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

        var classCount = 0;
        var secondClassLine = 0;
        for (var index = 0; index < input.Lines.Count; index++)
        {
            if (!ClassPattern.IsMatch(input.Lines[index]))
            {
                continue;
            }
            classCount++;
            if (classCount == 2)
            {
                secondClassLine = index + 1;
            }
        }

        if (classCount > 1 &&
            !IgnoreRules.IsIgnored(new IgnoreCheckInput(
                input.Path,
                "UPD402",
                string.Empty,
                input.IgnoreRules)))
        {
            findings.Add(new Finding(
                input.Path,
                secondClassLine,
                "UPD402",
                "file contains multiple major classes",
                "warning"));
        }

        return findings;
    }
}
