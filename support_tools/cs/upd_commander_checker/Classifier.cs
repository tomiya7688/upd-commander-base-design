namespace UpdCommanderChecker;

internal static class Classifier
{
    private static readonly HashSet<string> Layers = ["ui", "process", "data"];
    private static readonly HashSet<string> Roles =
    [
        "commander",
        "messenger",
        "processing",
        "compresser",
    ];
    private static readonly HashSet<string> AppRoots =
    [
        "app",
        "apps",
        "application",
        "applications",
        "feature",
        "features",
    ];

    internal static ModuleInfo ClassifyPath(string path)
    {
        var directories = PathDirectories(path);
        var scope = ApplicationScope(directories);
        var stem = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        return new ModuleInfo(
            path,
            FindName(new FindNameInput(scope, Layers)),
            FindPathRole(new FindPathRoleInput(scope, stem)),
            FindApplication(directories)
        );
    }

    internal static ModuleInfo ClassifyReference(string value)
    {
        var parts = SplitParts(value);
        var scope = ApplicationScope(parts);
        return new ModuleInfo(
            value,
            FindName(new FindNameInput(scope, Layers)),
            FindRole(scope),
            FindApplication(parts)
        );
    }

    private static List<string> PathDirectories(string value)
    {
        var directory = Path.GetDirectoryName(value) ?? string.Empty;
        return directory
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.ToLowerInvariant())
            .Where(part => part != ".")
            .ToList();
    }

    private static List<string> SplitParts(string value)
    {
        var normalized = value
            .ToLowerInvariant()
            .Replace('\\', '/')
            .Replace('.', '/')
            .Replace('-', '/')
            .Replace('_', '/');
        return normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static string FindName(FindNameInput input)
    {
        foreach (var part in input.Parts.Reverse())
        {
            if (input.Candidates.Contains(part))
            {
                return part;
            }
        }
        return string.Empty;
    }

    private static string FindPathRole(FindPathRoleInput input)
    {
        var directoryRole = FindName(new FindNameInput(input.Directories, Roles));
        if (!string.IsNullOrEmpty(directoryRole))
        {
            return directoryRole;
        }
        foreach (var role in Roles)
        {
            if (input.Stem == role || input.Stem.EndsWith("_" + role, StringComparison.Ordinal))
            {
                return role;
            }
        }
        return string.Empty;
    }

    private static string FindRole(IEnumerable<string> parts)
    {
        foreach (var part in parts.Reverse())
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
            if (part.EndsWith("compresser", StringComparison.Ordinal))
            {
                return "compresser";
            }
        }
        return string.Empty;
    }

    private static string FindApplication(IReadOnlyList<string> parts)
    {
        for (var index = parts.Count - 2; index >= 0; index--)
        {
            if (AppRoots.Contains(parts[index]))
            {
                return parts[index + 1];
            }
        }
        return string.Empty;
    }

    private static List<string> ApplicationScope(IReadOnlyList<string> parts)
    {
        for (var index = parts.Count - 2; index >= 0; index--)
        {
            if (AppRoots.Contains(parts[index]))
            {
                return parts.Skip(index + 2).ToList();
            }
        }
        return parts.ToList();
    }
}
