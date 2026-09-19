namespace UpdCommanderChecker;

internal sealed record ModelAttentionCollectInput(
    ParsedSource Source,
    ModuleInfo Module,
    IReadOnlyList<IgnoreRule> IgnoreRules,
    int MinItems
);

internal sealed record ModelAttentionAddInput(
    List<ModelGroupOccurrence> Occurrences,
    ParsedSource Source,
    ModuleInfo Module,
    IReadOnlyList<IgnoreRule> IgnoreRules,
    int MinItems,
    int Line,
    string Kind,
    IReadOnlyList<string> Items
);
