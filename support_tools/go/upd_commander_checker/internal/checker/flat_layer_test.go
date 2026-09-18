package checker

import (
	"fmt"
	"path/filepath"
	"testing"
)

func writeFlatLayerFiles(t *testing.T, root string, direct int, nested int, excluded int) {
	t.Helper()
	for index := 0; index < direct; index++ {
		writeTestFile(t, filepath.Join(root, "process", fmt.Sprintf("direct_%d.go", index)), "package process\n")
	}
	for index := 0; index < nested; index++ {
		writeTestFile(t, filepath.Join(root, "process", "group", fmt.Sprintf("nested_%d.go", index)), "package process\n")
	}
	for index := 0; index < excluded; index++ {
		writeTestFile(t, filepath.Join(root, "process", "generated", fmt.Sprintf("generated_%d.go", index)), "package process\n")
	}
}

func TestFlatLayerDefaultBoundary(t *testing.T) {
	below := t.TempDir()
	writeFlatLayerFiles(t, below, 9, 3, 0)
	assertNoCode(t, ScanPath(below, nil), "UPD405")

	over := t.TempDir()
	writeFlatLayerFiles(t, over, 10, 2, 0)
	findings := ScanPath(over, nil)
	assertHasCode(t, findings, "UPD405")
}

func TestFlatLayerSmallAndGeneratedHeavyDoNotTrigger(t *testing.T) {
	small := t.TempDir()
	writeFlatLayerFiles(t, small, 8, 0, 0)
	assertNoCode(t, ScanPath(small, nil), "UPD405")

	generated := t.TempDir()
	writeFlatLayerFiles(t, generated, 5, 0, 20)
	assertNoCode(t, ScanPath(generated, nil), "UPD405")
}

func TestFlatLayerCustomThresholds(t *testing.T) {
	root := t.TempDir()
	writeFlatLayerFiles(t, root, 6, 0, 0)
	assertHasCode(t, ScanPathWithThresholds(root, nil, 2, 6, 80), "UPD405")
}
