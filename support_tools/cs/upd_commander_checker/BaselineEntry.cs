namespace UpdCommanderChecker;

// {
// 責務: [BaselineEntry: fingerprintと表示用metadataを含むFindingを表す]
// フィールド: [Fingerprint/Rule/Path/Symbol/Context: identity, Severity/Line/Message: 表示情報]
// }
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
