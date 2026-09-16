namespace UpdCommanderChecker;

internal static class SingleFileContext
{
    private static readonly HashSet<string> ApplicationMarkers =
    [
        "app",
        "apps",
        "application",
        "applications",
        "feature",
        "features",
    ];

    private static readonly HashSet<string> Layers = ["ui", "process", "data"];

    internal static string FindRoot(string file)
    {
        var directory =
            Path.GetDirectoryName(Path.GetFullPath(file)) ?? Directory.GetCurrentDirectory();
        string? applicationRoot = null;
        for (
            var current = new DirectoryInfo(directory);
            current is not null;
            current = current.Parent
        )
        {
            if (ApplicationMarkers.Contains(current.Name.ToLowerInvariant()))
            {
                applicationRoot = current.Parent?.FullName ?? current.FullName;
            }
        }
        if (applicationRoot is not null)
        {
            return applicationRoot;
        }

        for (
            var current = new DirectoryInfo(directory);
            current is not null;
            current = current.Parent
        )
        {
            if (Layers.Contains(current.Name.ToLowerInvariant()))
            {
                return current.Parent?.FullName ?? directory;
            }
        }
        return directory;
    }
}
