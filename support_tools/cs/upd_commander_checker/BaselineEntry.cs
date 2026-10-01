namespace UpdCommanderChecker;

internal sealed record BaselineEntry(
    string Fingerprint,
    string Rule,
    string Path,
    string Symbol,
    string Context,
    string Severity,
    int Line = 0,
    string Message = ""
);
