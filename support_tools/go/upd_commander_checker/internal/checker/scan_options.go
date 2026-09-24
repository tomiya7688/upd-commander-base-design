package checker

type ScanOptions struct {
	Upd301MaxInputs           int
	FlatLayerMinFiles         int
	FlatLayerMinDirectPercent int
	ModelGroupMinItems        int
	ModelGroupMinOccurrences  int
	CommonRoots               []string
}

func DefaultScanOptions() ScanOptions {
	return ScanOptions{
		Upd301MaxInputs:           2,
		FlatLayerMinFiles:         12,
		FlatLayerMinDirectPercent: 80,
		ModelGroupMinItems:        3,
		ModelGroupMinOccurrences:  2,
		CommonRoots:               []string{"common", "shared"},
	}
}
