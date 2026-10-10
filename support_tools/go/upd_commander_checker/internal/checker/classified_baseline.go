package checker

// ClassifiedBaseline contains sorted status groups for a comparison.
// {
// 責務: [ClassifiedBaseline: baseline比較結果を3状態別に保持する]
// フィールド: [New: 新規Finding, Existing: 継続中Finding, Resolved: 解消済みFinding]
// }
type ClassifiedBaseline struct {
	New      []BaselineEntry `json:"new"`
	Existing []BaselineEntry `json:"existing"`
	Resolved []BaselineEntry `json:"resolved"`
}
