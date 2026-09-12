using System.Text.RegularExpressions;

namespace UpdCommanderChecker;

internal sealed record IgnoreRule(string Code, string Pattern);

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

    internal static bool IsIgnored(
        string path,
        string code,
        string lineText,
        IReadOnlyList<IgnoreRule> rules)
    {
        foreach (var rule in rules)
        {
            if (GlobMatch(path, rule.Pattern) && (rule.Code == "all" || rule.Code == code))
            {
                return true;
            }
        }
        return lineText.Contains($"upd: ignore {code}", StringComparison.Ordinal) ||
               lineText.Contains("upd: ignore all", StringComparison.Ordinal);
    }

    internal static bool GlobMatch(string path, string pattern)
    {
        var normalizedPath = path.Replace('\\', '/');
        var normalizedPattern = pattern.Replace('\\', '/');
        var regex = "^" + Regex.Escape(normalizedPattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", "[^/]") + "$";
        return Regex.IsMatch(normalizedPath, regex, RegexOptions.CultureInvariant);
    }
}
