namespace UpdCommanderChecker;

internal static class DependencyRules
{
    internal static string? GetError(ModuleInfo source, ModuleInfo target)
    {
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
        var path = target.Path.ToLowerInvariant();
        return path.Contains(".contract", StringComparison.Ordinal) ||
               path.Contains(".contracts", StringComparison.Ordinal) ||
               path.Contains(".dto", StringComparison.Ordinal) ||
               path.Contains(".dtos", StringComparison.Ordinal) ||
               path.Contains(".shared", StringComparison.Ordinal) ||
               path.Contains("/contract", StringComparison.Ordinal) ||
               path.Contains("/shared", StringComparison.Ordinal);
    }
}
