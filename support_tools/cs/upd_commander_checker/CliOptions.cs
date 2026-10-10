namespace UpdCommanderChecker;

internal sealed record CliOptions(
    string Target,
    string Output,
    IReadOnlyList<string> Ignores,
    bool WarningsAsErrors,
    bool AttentionsAsErrors,
    IReadOnlyList<string> FailOn,
    bool FailOnConfigured,
    IReadOnlyDictionary<string, string> SeverityOverrides,
    bool WriteBaseline = false,
    string WriteBaselinePath = "",
    string BaselinePath = ""
);
