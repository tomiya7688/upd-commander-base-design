namespace UpdCommanderChecker;

internal sealed record BaselineComparison(
    IReadOnlyList<BaselineEntry> New,
    IReadOnlyList<BaselineEntry> Existing,
    IReadOnlyList<BaselineEntry> Resolved
);
