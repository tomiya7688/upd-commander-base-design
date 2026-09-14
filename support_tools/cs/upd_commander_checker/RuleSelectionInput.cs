namespace UpdCommanderChecker;

internal sealed record RuleSelectionInput(
    IEnumerable<Finding> Findings,
    IReadOnlyCollection<string>? EnabledRules
);
