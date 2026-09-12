namespace UpdCommanderChecker;

internal static class Classifier
{
    private static readonly HashSet<string> Layers = ["ui", "process", "data"];
    private static readonly HashSet<string> Roles = ["commander", "messenger", "processing"];
    private static readonly HashSet<string> AppRoots = ["app", "apps", "application", "applications", "feature", "features"];

    internal static ModuleInfo ClassifyPath(string path) => Classify(path);

    internal static ModuleInfo ClassifyReference(string value) => Classify(value);

    private static ModuleInfo Classify(string value)
    {
        var parts = SplitParts(value);
        return new ModuleInfo(
            value,
            FindName(parts, Layers),
            FindRole(parts),
            FindApplication(parts));
    }

    private static List<string> SplitParts(string value)
    {
        var normalized = value.ToLowerInvariant()
            .Replace('\\', '/')
            .Replace('.', '/')
            .Replace('-', '/')
            .Replace('_', '/');
        return normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static string FindName(IEnumerable<string> parts, HashSet<string> candidates)
    {
        foreach (var part in parts)
        {
            if (candidates.Contains(part))
            {
                return part;
            }
        }
        return string.Empty;
    }

    private static string FindRole(IEnumerable<string> parts)
    {
        foreach (var part in parts)
        {
            if (Roles.Contains(part))
            {
                return part;
            }
            if (part.EndsWith("commander", StringComparison.Ordinal))
            {
                return "commander";
            }
            if (part.EndsWith("messenger", StringComparison.Ordinal))
            {
                return "messenger";
            }
            if (part.EndsWith("processing", StringComparison.Ordinal))
            {
                return "processing";
            }
        }
        return string.Empty;
    }

    private static string FindApplication(IReadOnlyList<string> parts)
    {
        for (var index = 0; index + 1 < parts.Count; index++)
        {
            if (AppRoots.Contains(parts[index]))
            {
                return parts[index + 1];
            }
        }
        return string.Empty;
    }
}
