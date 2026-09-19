package checker

import (
	"fmt"
	"path/filepath"
	"testing"
)

type flatLayerFixtureInput struct {
	root     string
	direct   int
	nested   int
	excluded int
}

func writeFlatLayerFiles(t *testing.T, input flatLayerFixtureInput) {
	t.Helper()
	for index := 0; index < input.direct; index++ {
		writeTestFile(t, filepath.Join(input.root, "process", fmt.Sprintf("direct_%d.go", index)), "package process\n")
	}
	for index := 0; index < input.nested; index++ {
		writeTestFile(t, filepath.Join(input.root, "process", "group", fmt.Sprintf("nested_%d.go", index)), "package process\n")
	}
	for index := 0; index < input.excluded; index++ {
		writeTestFile(t, filepath.Join(input.root, "process", "generated", fmt.Sprintf("generated_%d.go", index)), "package process\n")
	}
}

func TestFlatLayerDefaultBoundary(t *testing.T) {
	below := t.TempDir()
	writeFlatLayerFiles(t, flatLayerFixtureInput{root: below, direct: 9, nested: 3})
	assertNoCode(t, ScanPath(below, nil), "UPD405")

	over := t.TempDir()
	writeFlatLayerFiles(t, flatLayerFixtureInput{root: over, direct: 10, nested: 2})
	findings := ScanPath(over, nil)
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD405"})
}

func TestFlatLayerSmallAndGeneratedHeavyDoNotTrigger(t *testing.T) {
	small := t.TempDir()
	writeFlatLayerFiles(t, flatLayerFixtureInput{root: small, direct: 8})
	assertNoCode(t, ScanPath(small, nil), "UPD405")

	generated := t.TempDir()
	writeFlatLayerFiles(t, flatLayerFixtureInput{root: generated, direct: 5, excluded: 20})
	assertNoCode(t, ScanPath(generated, nil), "UPD405")
}

func TestFlatLayerCustomThresholds(t *testing.T) {
	root := t.TempDir()
	writeFlatLayerFiles(t, flatLayerFixtureInput{root: root, direct: 6})
	assertHasCode(t, ScanPathWithThresholds(root, nil, 2, 6, 80), "UPD405")
}
