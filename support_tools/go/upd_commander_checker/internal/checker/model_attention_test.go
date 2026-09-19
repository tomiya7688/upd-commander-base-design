package checker

import (
	"path/filepath"
	"strings"
	"testing"
)

func TestModelAttentionRepeatedParameters(t *testing.T) {
	root := t.TempDir()
	writeTestFile(
		t,
		filepath.Join(root, "process", "sample.go"),
		"package process\nfunc first(id int, name string, email string) {}\nfunc second(id int, name string, email string) {}\n",
	)
	findings := ScanPath(root, nil)
	found := false
	for _, finding := range findings {
		if finding.Code == "UPD406" {
			found = true
			if !strings.Contains(finding.Message, "items=id,name,email") ||
				!strings.Contains(finding.Message, "occurrences=2") ||
				!strings.Contains(finding.Message, "kind=parameters") {
				t.Fatalf("unexpected message: %s", finding.Message)
			}
		}
	}
	if !found {
		t.Fatal("expected UPD406")
	}
}

func TestModelAttentionTwoItemsOrderAndCrossLayerDoNotTrigger(t *testing.T) {
	root := t.TempDir()
	writeTestFile(
		t,
		filepath.Join(root, "process", "two.go"),
		"package process\nfunc pairOne(id int, name string) {}\nfunc pairTwo(id int, name string) {}\n",
	)
	writeTestFile(
		t,
		filepath.Join(root, "process", "order.go"),
		"package process\nfunc orderOne(id int, name string, email string) {}\nfunc orderTwo(email string, name string, id int) {}\n",
	)
	writeTestFile(
		t,
		filepath.Join(root, "ui", "cross.go"),
		"package ui\nfunc cross(id int, name string, email string) {}\n",
	)
	assertNoCode(codeAssertionInput{t: t, findings: ScanPath(root, nil), code: "UPD406"})
}

func TestModelAttentionTupleAndPerformanceIgnore(t *testing.T) {
	tupleRoot := t.TempDir()
	writeTestFile(
		t,
		filepath.Join(tupleRoot, "process", "tuple.go"),
		"package process\nfunc first() (int,int,int) { return x, y, z }\nfunc second() (int,int,int) { return x, y, z }\n",
	)
	findings := ScanPath(tupleRoot, nil)
	foundTuple := false
	for _, finding := range findings {
		if finding.Code == "UPD406" && strings.Contains(finding.Message, "kind=tuple") {
			foundTuple = true
		}
	}
	if !foundTuple {
		t.Fatal("expected tuple UPD406")
	}

	ignoredRoot := t.TempDir()
	writeTestFile(
		t,
		filepath.Join(ignoredRoot, "process", "parallel.go"),
		"package process\nfunc one(xs,ys,zs []int, i int) { _ = xs[i] + ys[i] + zs[i] }\nfunc two(xs,ys,zs []int, i int) { _ = xs[i] + ys[i] + zs[i] }\n",
	)
	writeTestFile(t, filepath.Join(ignoredRoot, ".updcommanderignore"), "UPD406 process/parallel.go\n")
	assertNoCode(codeAssertionInput{t: t, findings: ScanPath(ignoredRoot, nil), code: "UPD406"})
}

func TestModelAttentionCustomThresholds(t *testing.T) {
	root := t.TempDir()
	writeTestFile(
		t,
		filepath.Join(root, "process", "sample.go"),
		"package process\nfunc first(id int, name string, email string) {}\nfunc second(id int, name string, email string) {}\n",
	)
	assertNoCode(codeAssertionInput{t: t, findings: ScanPathWithAllThresholds(root, nil, 2, 12, 80, 4, 2), code: "UPD406"})
}
