namespace UpdCommanderChecker;

internal sealed record ScanFileInput(
    string File,
    string Relative,
    IReadOnlyList<IgnoreRule> IgnoreRules
);
