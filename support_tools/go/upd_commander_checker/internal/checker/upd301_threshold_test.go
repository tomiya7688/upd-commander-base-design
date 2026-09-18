package checker

import (
	"path/filepath"
	"testing"
)

func TestUpd301DefaultThresholdBoundary(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "process", "threshold_processing.go")
	writeTestFile(
		t,
		path,
		"package process\ntype Worker struct{}\nfunc (Worker) Good(left, right int) {}\nfunc (Worker) Bad(left, right, mode int) {}\n",
	)

	findings := ScanPath(root, nil)
	if countFindingCode(findings, "UPD301") != 1 {
		t.Fatalf("expected one UPD301 at boundary+1: %+v", findings)
	}
}

func TestUpd301CustomThresholdBoundary(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "process", "custom_processing.go")
	writeTestFile(
		t,
		path,
		"package process\ntype Worker struct{}\nfunc (Worker) Two(left, right int) {}\nfunc (Worker) Three(left, right, mode int) {}\nfunc (Worker) Four(left, right, mode, extra int) {}\n",
	)

	strict := ScanPathWithUpd301MaxInputs(root, nil, 1)
	if countFindingCode(strict, "UPD301") != 3 {
		t.Fatalf("expected three strict UPD301 findings: %+v", strict)
	}

	relaxed := ScanPathWithUpd301MaxInputs(root, nil, 3)
	if countFindingCode(relaxed, "UPD301") != 1 {
		t.Fatalf("expected one relaxed UPD301 finding: %+v", relaxed)
	}
}

func TestUpd301VariadicParameterCountsAsOneInput(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "process", "variadic_processing.go")
	writeTestFile(
		t,
		path,
		"package process\ntype Worker struct{}\nfunc (Worker) Good(left int, values ...int) {}\nfunc (Worker) Bad(left, right int, values ...int) {}\n",
	)

	findings := ScanPath(root, nil)
	if countFindingCode(findings, "UPD301") != 1 {
		t.Fatalf("expected variadic declaration to count as one input: %+v", findings)
	}
}

func countFindingCode(findings []Finding, code string) int {
	count := 0
	for _, finding := range findings {
		if finding.Code == code {
			count++
		}
	}
	return count
}
