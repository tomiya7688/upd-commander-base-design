namespace UpdCommanderChecker;

internal sealed record DataTypeLocationRuleContext(
    IReadOnlyList<string> Files,
    string Root,
    IReadOnlyList<IgnoreRule> IgnoreRules
);
