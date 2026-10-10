package main

import (
	"os"
	"path/filepath"
	"strings"
	"testing"

	"upd_commander_checker/internal/checker"
)

func TestBaselineCLIWriteCompareAndResolve(t *testing.T) {
	root := t.TempDir()
	source := filepath.Join(root, "commander.go")
	report := filepath.Join(root, "report.txt")
	baseline := filepath.Join(root, ".upd-baseline.json")
	writeSource := func(contents string) {
		t.Helper()
		if err := os.WriteFile(source, []byte(contents), 0o644); err != nil {
			t.Fatal(err)
		}
	}
	enabledRules := []string{"UPD203"}
	config := checker.Config{Input: root, Output: report, EnabledRules: &enabledRules}
	writeSource("package commander\nimport \"os\"\nfunc Commander() {\n os.Exit(0)\n}\n")
	if code := runCLI([]string{"--write-baseline"}, config); code != 1 {
		t.Fatalf("expected finding exit code 1, got %d", code)
	}
	if _, err := os.Stat(baseline); err != nil {
		t.Fatalf("baseline was not written: %v", err)
	}
	writeSource("\npackage commander\nimport \"os\"\nfunc Commander() {\n os.Exit(0)\n}\n")
	if code := runCLI([]string{"--baseline", baseline}, config); code != 1 {
		t.Fatalf("expected existing finding exit code 1, got %d", code)
	}
	data, err := os.ReadFile(report)
	if err != nil {
		t.Fatal(err)
	}
	if !strings.Contains(string(data), "EXISTING E UPD203") {
		t.Fatalf("expected EXISTING finding in report, got %s", data)
	}
	writeSource("package commander\nimport \"os\"\nfunc Commander() {\n os.Exit(0)\n os.Remove(\"x\")\n}\n")
	if code := runCLI([]string{"--baseline", baseline}, config); code != 1 {
		t.Fatalf("expected new finding exit code 1, got %d", code)
	}
	data, err = os.ReadFile(report)
	if err != nil {
		t.Fatal(err)
	}
	if !strings.Contains(string(data), "EXISTING E UPD203") || !strings.Contains(string(data), "NEW E UPD203") {
		t.Fatalf("expected EXISTING and NEW findings in report, got %s", data)
	}
	writeSource("package commander\nfunc Commander() {}\n")
	if code := runCLI([]string{"--baseline", baseline}, config); code != 0 {
		t.Fatalf("expected resolved-only success exit code 0, got %d", code)
	}
	data, err = os.ReadFile(report)
	if err != nil {
		t.Fatal(err)
	}
	if strings.Count(string(data), "RESOLVED E UPD203") != 1 {
		t.Fatalf("expected RESOLVED finding in report, got %s", data)
	}
}

func TestBaselineCLIRejectsCorruptAndConflictingFlags(t *testing.T) {
	root := t.TempDir()
	input := filepath.Join(root, "source.go")
	if err := os.WriteFile(input, []byte("package sample\n"), 0o644); err != nil {
		t.Fatal(err)
	}
	config := checker.Config{Input: input}
	if code := runCLI([]string{"--write-baseline", "--baseline", "broken.json"}, config); code != 2 {
		t.Fatalf("expected conflicting option exit code 2, got %d", code)
	}
	if code := runCLI([]string{"--baseline", filepath.Join(root, "missing.json")}, config); code != 2 {
		t.Fatalf("expected malformed/missing baseline exit code 2, got %d", code)
	}
}
