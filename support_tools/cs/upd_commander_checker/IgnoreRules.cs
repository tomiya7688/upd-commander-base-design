using System.Text.RegularExpressions;

namespace UpdCommanderChecker;

internal static class IgnoreRules
{
    internal static IReadOnlyList<IgnoreRule> Load(string root)
    {
        var path = Path.Combine(root, ".updcommanderignore");
        if (!File.Exists(path))
        {
            return [];
        }

        var rules = new List<IgnoreRule>();
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }
            var mainPart = line.Split('#', 2)[0].Trim();
            var fields = mainPart.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length == 1)
            {
                rules.Add(new IgnoreRule("all", fields[0]));
            }
            else if (fields.Length >= 2)
            {
                rules.Add(new IgnoreRule(fields[0], fields[1]));
            }
        }
        return rules;
    }

    internal static bool IsIgnored(IgnoreCheckInput input)
    {
        foreach (var rule in input.Rules)
        {
            if (
                GlobMatch(new GlobMatchInput(input.Path, rule.Pattern))
                && (rule.Code == "all" || rule.Code == input.Code)
            )
            {
                return true;
            }
        }
        return input.LineText.Contains($"upd: ignore {input.Code}", StringComparison.Ordinal)
            || input.LineText.Contains("upd: ignore all", StringComparison.Ordinal);
    }

    internal static bool GlobMatch(GlobMatchInput input)
    {
        var normalizedPath = input.Path.Replace('\\', '/');
        var normalizedPattern = input.Pattern.Replace('\\', '/');
        var regex =
            "^"
            + Regex
                .Escape(normalizedPattern)
                .Replace("\\*\\*", ".*")
                .Replace("\\*", "[^/]*")
                .Replace("\\?", "[^/]")
            + "$";
        return Regex.IsMatch(normalizedPath, regex, RegexOptions.CultureInvariant);
    }
}
