namespace UpdCommanderChecker;

internal static class FlatLayerRules
{
    private static readonly HashSet<string> ExcludedDirectories =
    [
        "generated",
        "third_party",
        "vendor",
        "external",
        "build",
    ];

    private static readonly HashSet<string> ApplicationMarkers =
    [
        "app",
        "apps",
        "application",
        "applications",
        "feature",
        "features",
    ];

    private static readonly HashSet<string> LayerNames = ["ui", "process", "data"];

    internal static IEnumerable<Finding> Check(
        (
            IReadOnlyList<string> Files,
            string Root,
            IReadOnlyList<IgnoreRule> IgnoreRules,
            int MinFiles,
            int MinDirectPercent
        ) input
    )
    {
        var counts = new Dictionary<string, (int Total, int Direct)>(StringComparer.Ordinal);
        foreach (var file in input.Files)
        {
            var relative = Path.GetRelativePath(input.Root, file).Replace('\\', '/');
            var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (
                parts
                    .Take(Math.Max(0, parts.Length - 1))
                    .Select(part => part.ToLowerInvariant())
                    .Any(ExcludedDirectories.Contains)
            )
            {
                continue;
            }

            var layerIndex = LayerRootIndex(parts);
            if (layerIndex < 0)
            {
                continue;
            }
            var layerRoot = string.Join('/', parts.Take(layerIndex + 1));
            counts.TryGetValue(layerRoot, out var count);
            count.Total++;
            if (parts.Length == layerIndex + 2)
            {
                count.Direct++;
            }
            counts[layerRoot] = count;
        }

        foreach (var (layerRoot, count) in counts)
        {
            if (
                count.Total < input.MinFiles
                || count.Direct * 100 < count.Total * input.MinDirectPercent
                || IgnoreRules.IsIgnored(
                    new IgnoreCheckInput(layerRoot, "UPD405", string.Empty, input.IgnoreRules)
                )
            )
            {
                continue;
            }
            yield return new Finding(
                layerRoot,
                1,
                "UPD405",
                "large flat layer reduces navigability; consider grouping related responsibilities",
                "attention"
            );
        }
    }

    private static int LayerRootIndex(IReadOnlyList<string> parts)
    {
        var scopeStart = 0;
        for (var index = 0; index + 2 < parts.Count; index++)
        {
            if (ApplicationMarkers.Contains(parts[index].ToLowerInvariant()))
            {
                scopeStart = index + 2;
            }
        }

        var layerIndex = -1;
        for (var index = scopeStart; index + 1 < parts.Count; index++)
        {
            if (LayerNames.Contains(parts[index].ToLowerInvariant()))
            {
                layerIndex = index;
            }
        }
        return layerIndex;
    }
}
