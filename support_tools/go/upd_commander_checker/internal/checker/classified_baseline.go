package checker

// ClassifiedBaseline contains sorted status groups for a comparison.
type ClassifiedBaseline struct {
	New      []BaselineEntry `json:"new"`
	Existing []BaselineEntry `json:"existing"`
	Resolved []BaselineEntry `json:"resolved"`
}
