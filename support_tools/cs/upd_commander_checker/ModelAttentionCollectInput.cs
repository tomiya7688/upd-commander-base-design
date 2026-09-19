namespace UpdCommanderChecker;

internal sealed record ModelAttentionCollectInput(
    ParsedSource Source,
    ModuleInfo Module,
    IReadOnlyList<IgnoreRule> IgnoreRules,
    int MinItems
);
