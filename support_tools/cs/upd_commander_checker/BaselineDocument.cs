namespace UpdCommanderChecker;

// {
// 責務: [BaselineDocument: baseline JSON文書のversionとFinding集合を保持する]
// フィールド: [SchemaVersion/FingerprintVersion: 対応契約の版, Findings: 保存済みFinding]
// }
internal sealed record BaselineDocument(
    int SchemaVersion,
    int FingerprintVersion,
    IReadOnlyList<BaselineEntry> Findings
);
