namespace UpdCommanderChecker;

internal static class DependencyRules
{
    private static readonly HashSet<string> BoundaryApiNames =
    [
        "contract",
        "contracts",
        "dto",
        "dtos",
        "message",
        "messages",
    ];

    internal static DependencyRuleResult? Evaluate(DependencyCheckInput input)
    {
        var source = input.Source;
        var target = input.Target;

        if (
            !string.IsNullOrEmpty(source.ApplicationId)
            && !string.IsNullOrEmpty(target.ApplicationId)
            && source.ApplicationId != target.ApplicationId
            && !IsBoundaryApi(target)
        )
        {
            return new DependencyRuleResult(
                "UPD102",
                "cross-application internal dependency",
                "error"
            );
        }
        if (
            source.Layer == "common"
            && target.Layer is "ui" or "process" or "data"
        )
        {
            return new DependencyRuleResult(
                "UPD101",
                "Common/Shared must not depend on layer-specific implementation",
                "error"
            );
        }
        if (source.Layer == "ui" && target.Layer == "data")
        {
            return new DependencyRuleResult("UPD101", "UI must not depend on Data", "error");
        }
        if (source.Layer == "data" && target.Layer == "ui")
        {
            return new DependencyRuleResult("UPD101", "Data must not depend on UI", "error");
        }
        if (
            source.Layer != "common"
            && target.Layer != "common"
            && source.Role == "messenger"
            && target.Role == "processing"
        )
        {
            return new DependencyRuleResult(
                "UPD101",
                "Messenger must not depend on Processing",
                "error"
            );
        }
        if (
            source.Layer != "common"
            && target.Layer != "common"
            && source.Role == "processing"
            && target.Role == "processing"
        )
        {
            return new DependencyRuleResult(
                "UPD101",
                "Processing must not depend on Processing",
                "error"
            );
        }
        if (
            source.Role == "commander"
            && source.Layer != "common"
            && target.Layer != "common"
            && target.Role == "processing"
            && !string.IsNullOrEmpty(source.Layer)
            && !string.IsNullOrEmpty(target.Layer)
            && source.Layer != target.Layer
        )
        {
            return new DependencyRuleResult(
                "UPD101",
                "Commander must not depend on Processing in another layer",
                "error"
            );
        }
        if (
            source.Layer == "data"
            && source.Role == "commander"
            && target.Layer == "data"
            && target.Role == "commander"
        )
        {
            return new DependencyRuleResult(
                "UPD103",
                "Data Commander should not communicate directly with another Data Commander",
                "warning"
            );
        }
        return null;
    }

    private static bool IsBoundaryApi(ModuleInfo target)
    {
        if (target.Role == "messenger")
        {
            return true;
        }

        var parts = target
            .Path.ToLowerInvariant()
            .Split(['.', '/', '\\', '-', '_'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(BoundaryApiNames.Contains);
    }
}
