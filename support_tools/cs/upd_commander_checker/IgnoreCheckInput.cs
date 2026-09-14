namespace UpdCommanderChecker;

internal sealed record IgnoreCheckInput(
    string Path,
    string Code,
    string LineText,
    IReadOnlyList<IgnoreRule> Rules
);
