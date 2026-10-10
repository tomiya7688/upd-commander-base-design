namespace UpdCommanderChecker;

// {
// 責務: [BaselineComparison: FindingをNEW/EXISTING/RESOLVEDへ分けた結果を保持する]
// フィールド: [New/Existing/Resolved: 各差分状態のFinding一覧]
// }
internal sealed record BaselineComparison(
    IReadOnlyList<BaselineEntry> New,
    IReadOnlyList<BaselineEntry> Existing,
    IReadOnlyList<BaselineEntry> Resolved
);
