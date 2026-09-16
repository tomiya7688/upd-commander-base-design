namespace UpdCommanderChecker;

internal sealed record CliOptions(
    string Target,
    string Output,
    IReadOnlyList<string> Ignores,
    bool WarningsAsErrors,
    bool AttentionsAsErrors
);
