namespace UpdCommanderChecker;

internal static class DependencyRules
{
    private static readonly HashSet<string> BoundaryApiNames =
        ["contract", "contracts", "dto", "dtos", "shared"];

    internal static string? GetError(DependencyCheckInput input)
    {
        var source = input.Source;
        var target = input.Target;

        if (!string.IsNullOrEmpty(source.ApplicationId) &&
            !string.IsNullOrEmpty(target.ApplicationId) &&
            source.ApplicationId != target.ApplicationId &&
            !IsBoundaryApi(target))
        {
            return "cross-application internal dependency";
        }
        if (source.Layer == "ui" && target.Layer == "data")
        {
            return "UI must not depend on Data";
        }
        if (source.Layer == "data" && target.Layer == "ui")
        {
            return "Data must not depend on UI";
        }
        if (source.Role == "messenger" && target.Role == "processing")
        {
            return "Messenger must not depend on Processing";
        }
        if (source.Role == "processing" && target.Role == "processing")
        {
            return "Processing must not depend on Processing";
        }
        if (source.Role == "commander" && target.Role == "processing" &&
            !string.IsNullOrEmpty(source.Layer) && !string.IsNullOrEmpty(target.Layer) &&
            source.Layer != target.Layer)
        {
            return "Commander must not depend on Processing in another layer";
        }
        return null;
    }

    private static bool IsBoundaryApi(ModuleInfo target)
    {
        if (target.Role == "messenger")
        {
            return true;
        }

        var parts = target.Path
            .ToLowerInvariant()
            .Split(['.', '/', '\\', '-', '_'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(BoundaryApiNames.Contains);
    }
}
