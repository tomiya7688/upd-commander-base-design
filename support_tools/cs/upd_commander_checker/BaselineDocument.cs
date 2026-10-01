namespace UpdCommanderChecker;

internal sealed record BaselineDocument(
    int SchemaVersion,
    int FingerprintVersion,
    IReadOnlyList<BaselineEntry> Findings
);
